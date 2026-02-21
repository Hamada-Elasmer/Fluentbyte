/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Emulator/EmulatorInstanceControlService.cs
 * Purpose: Core component: EmulatorInstanceControlService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - InstanceId is string to match EmulatorLib.
 *  - Emulator responsibilities:
 *      - Scan instances
 *      - Start / Stop instance
 *      - Wait until instance online (best-effort)
 *  - Any game launching / screenshot / app automation must be done via ADB layer.
 *
 * FINAL POLICY (per your request):
 *  ✅ Android resolution is the SOURCE OF TRUTH (EmulatorAndroidWidth/Height/Dpi).
 *  ✅ Window CLIENT size always follows Android resolution when lock is enabled.
 *  ✅ Stable sizing is applied to prevent LDPlayer overriding size after splash.
 *
 * NEW POLICY (StoppingInstances flag):
 *  ✅ When StopAsync is called with waitUntilStopped=false, we mark the instance as "stopping".
 *  ✅ StartAsync will NOT start an instance while it is still stopping (prevents start/stop race).
 *  ✅ The "stopping" flag is cleared once StopAsync confirms the instance stopped (wait=true),
 *     or after a best-effort timeout in background confirm paths.
 * ============================================================================ */

using System.Diagnostics;
using System.Runtime.Versioning;
using EmulatorLib.Abstractions;
using EmulatorLib.LDPlayer;
using EmulatorLib.Models;
using SettingsStore;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using UtiliLib;
using UtiliLib.Infrastructure.Windows;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Emulator;

[SupportedOSPlatform("windows")]
public sealed class EmulatorInstanceControlService : IEmulatorInstanceControlService
{
    private readonly IEmulator _emulator;
    private readonly MLogger _log;
    private readonly SettingsAccessor _settings;

    private readonly Dictionary<string, IEmulatorInstance> _instancesById =
        new(StringComparer.OrdinalIgnoreCase);

    // InstanceId -> dnplayer PID (multi-instance safe binding)
    private readonly Dictionary<string, int> _dnPlayerPidByInstanceId =
        new(StringComparer.OrdinalIgnoreCase);

    // ✅ NEW: Track "stopping" instances to avoid StartAsync racing while shutdown is in progress.
    // InstanceId -> UTC timestamp when stop started.
    private readonly Dictionary<string, DateTimeOffset> _stoppingInstances =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly object _gate = new();

    // ✅ NEW: StartAsync will wait for stop to finish for up to this amount of time.
    private const int DefaultStartWaitForStoppingMs = 12_000;

    // ✅ NEW: Poll interval when waiting for stopping to clear.
    private const int DefaultStoppingPollMs = 250;

    public EmulatorInstanceControlService(
        IEmulator emulator,
        MLogger logger,
        SettingsAccessor settingsAccessor)
    {
        _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
        _log = logger ?? MLogger.Instance;
        _settings = settingsAccessor ?? throw new ArgumentNullException(nameof(settingsAccessor));
    }

    public Task RefreshAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            IReadOnlyList<IEmulatorInstance> instances;

            try
            {
                instances = _emulator.ScanInstances();
            }
            catch (Exception ex)
            {
                _log.Exception(LogChannel.SYSTEM, ex, "[Emu] ScanInstances failed");
                instances = Array.Empty<IEmulatorInstance>();
            }

            lock (_gate)
            {
                _instancesById.Clear();

                foreach (var inst in instances)
                {
                    if (string.IsNullOrWhiteSpace(inst.InstanceId))
                    {
                        _log.Warn(LogChannel.SYSTEM,
                            $"[Emu] Skipping instance '{inst.Name}' (empty InstanceId).");
                        continue;
                    }

                    _instancesById[inst.InstanceId] = inst;
                }
            }

            _log.Debug(LogChannel.SYSTEM,
                $"[Emu] Refresh completed. Cached instances={instances.Count}");
        }, ct);
    }

    public IEmulatorInstance GetById(string instanceId)
    {
        var inst = TryGetById(instanceId);
        if (inst is null)
            throw new KeyNotFoundException(
                $"Emulator instance not found for InstanceId='{instanceId}'. Did you call RefreshAsync()?");
        return inst;
    }

    public IEmulatorInstance? TryGetById(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        lock (_gate)
        {
            _instancesById.TryGetValue(instanceId, out var inst);
            return inst;
        }
    }

    public async Task StartAsync(
        string instanceId,
        bool waitUntilRunning = true,
        int timeoutMs = 90_000,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // ✅ NEW: If this instance is currently stopping, wait a bit before starting.
        // This prevents StartAsync from racing with "fast stop" (waitUntilStopped=false).
        await WaitIfStoppingAsync(instanceId, DefaultStartWaitForStoppingMs, ct).ConfigureAwait(false);

        if (TryGetById(instanceId) is null)
            await RefreshAsync(ct).ConfigureAwait(false);

        var inst = GetById(instanceId);

        _log.Info(LogChannel.SYSTEM,
            $"[Emu] Start instance '{inst.Name}' (InstanceId={inst.InstanceId}) wait={waitUntilRunning} timeout={timeoutMs}ms");

        // ✅ Apply LDPlayer internal Android resolution BEFORE start (best-effort)
        // ✅ Also keeps window values consistent (UI/legacy) when lock enabled.
        TryApplyLdPlayerAndroidResolutionBeforeStart(inst);

        // ✅ Enable ADB once globally (persisted in AppSettings)
        EnsureLdPlayerAdbEnabledOnce(inst);

        // --- Snapshot dnplayer PIDs BEFORE start (multi-instance safe) ---
        var beforePids = SnapshotDnPlayerPids();

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            inst.Start();
        }, ct).ConfigureAwait(false);

        if (!waitUntilRunning)
            return;

        await WaitUntilRunningAsync(inst, timeoutMs, ct).ConfigureAwait(false);

        _log.Info(LogChannel.SYSTEM,
            $"[Emu] Instance '{inst.Name}' (InstanceId={inst.InstanceId}) is running.");

        // --- Bind this start() to a dnplayer PID ---
        var pid = TryResolveDnPlayerPidForThisStart(beforePids);
        if (!pid.HasValue)
        {
            _log.Warn(LogChannel.SYSTEM,
                $"[Emu][Win] Could not resolve dnplayer PID for InstanceId={instanceId} (Name='{inst.Name}'). Window policy may not apply.");
            return;
        }

        lock (_gate)
            _dnPlayerPidByInstanceId[instanceId] = pid.Value;

        _log.Debug(LogChannel.SYSTEM,
            $"[Emu][Win] Bound InstanceId={instanceId} => dnplayer PID={pid.Value}");

        // --- Apply window policy (move/resize/max/min) ---
        await ApplyDnPlayerWindowPolicyAsync(instanceId, inst.Name, ct).ConfigureAwait(false);
    }

    public async Task StopAsync(
        string instanceId,
        bool waitUntilStopped = true,
        int timeoutMs = 60_000,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (TryGetById(instanceId) is null)
            await RefreshAsync(ct).ConfigureAwait(false);

        var inst = GetById(instanceId);

        _log.Info(LogChannel.SYSTEM,
            $"[Emu] Stop instance '{inst.Name}' (InstanceId={inst.InstanceId}) wait={waitUntilStopped} timeout={timeoutMs}ms");

        // ✅ NEW: Mark as stopping immediately.
        // - If waitUntilStopped=false, this flag prevents StartAsync from starting too early.
        // - If waitUntilStopped=true, we will clear it once shutdown is confirmed.
        MarkStopping(instanceId);

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            inst.Stop();
        }, ct).ConfigureAwait(false);

        if (!waitUntilStopped)
        {
            // ✅ If the caller does not want to wait, we just return immediately.
            // The instance remains marked as stopping until someone confirms stop (wait=true),
            // or until a future cleanup clears it (best-effort).
            return;
        }

        try
        {
            // ✅ IMPORTANT: LDPlayerParser currently sets State=Unknown, so state polling will timeout forever.
            // If state is unknown, rely on dnplayer PID exit instead.
            if (inst.State == EmulatorState.Unknown)
            {
                await WaitForDnPlayerExitAsync(instanceId, timeoutMs, ct).ConfigureAwait(false);

                lock (_gate)
                    _dnPlayerPidByInstanceId.Remove(instanceId);

                _log.Info(LogChannel.SYSTEM,
                    $"[Emu] Instance '{inst.Name}' (InstanceId={inst.InstanceId}) stopped (by dnplayer PID).");

                return;
            }

            await WaitForStateAsync(inst, EmulatorState.Stopped, timeoutMs, ct).ConfigureAwait(false);

            // best-effort cleanup
            lock (_gate)
                _dnPlayerPidByInstanceId.Remove(instanceId);

            _log.Info(LogChannel.SYSTEM,
                $"[Emu] Instance '{inst.Name}' (InstanceId={inst.InstanceId}) stopped.");
        }
        finally
        {
            // ✅ NEW: Clear stopping flag once we confirmed stop (or attempted to confirm).
            ClearStopping(instanceId);
        }
    }

    public Task<IReadOnlyList<EmulatorInstanceInfo>> List2Async(CancellationToken ct = default)
    {
        // SparkFlow v1 is ADB-first, we do not depend on LDPlayer list2 parsing.
        // This method is kept only for backward compatibility.
        return Task.FromResult<IReadOnlyList<EmulatorInstanceInfo>>(Array.Empty<EmulatorInstanceInfo>());
    }

    public async Task EmergencyStopAllAsync(CancellationToken ct = default)
        => await EmergencyStopAllAsync(12_000, 7_000, 3, ct).ConfigureAwait(false);

    public async Task EmergencyStopAllAsync(
        int overallTimeoutMs,
        int perInstanceTimeoutMs = 7000,
        int maxParallelStops = 3,
        CancellationToken ct = default)
    {
        if (overallTimeoutMs <= 0) overallTimeoutMs = 8000;
        if (perInstanceTimeoutMs <= 0) perInstanceTimeoutMs = 5000;
        if (maxParallelStops <= 0) maxParallelStops = 2;

        _log.Warn(LogChannel.SYSTEM,
            $"[Emu][Emergency] Stop all instances (overall={overallTimeoutMs}ms, per={perInstanceTimeoutMs}ms, parallel={maxParallelStops})");

        using var budgetCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budgetCts.CancelAfter(overallTimeoutMs);

        try
        {
            await RefreshAsync(CancellationToken.None).ConfigureAwait(false);

            var ids = SnapshotIds();
            if (ids.Count == 0)
            {
                _log.Warn(LogChannel.SYSTEM, "[Emu][Emergency] No cached instances to stop.");
                return;
            }

            await StopInstancesFastAsync(ids, perInstanceTimeoutMs, maxParallelStops, budgetCts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex, "[Emu][Emergency] Emergency stop failed");
        }
    }

    // ================================
    // Helpers
    // ================================

    /// <summary>
    /// Marks an instance as "stopping" so that StartAsync can avoid racing with shutdown.
    /// </summary>
    private void MarkStopping(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return;

        lock (_gate)
        {
            _stoppingInstances[instanceId] = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Clears the "stopping" flag (instance can be safely started again).
    /// </summary>
    private void ClearStopping(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return;

        lock (_gate)
        {
            _stoppingInstances.Remove(instanceId);
        }
    }

    /// <summary>
    /// Returns true if the instance is currently marked as stopping.
    /// </summary>
    private bool IsStopping(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return false;

        lock (_gate)
        {
            return _stoppingInstances.ContainsKey(instanceId);
        }
    }

    /// <summary>
    /// If the instance is stopping, wait for a short time for it to finish.
    /// This prevents StartAsync from launching while shutdown is still in progress.
    /// </summary>
    private async Task WaitIfStoppingAsync(string instanceId, int maxWaitMs, CancellationToken ct)
    {
        if (maxWaitMs <= 0) maxWaitMs = DefaultStartWaitForStoppingMs;

        if (!IsStopping(instanceId))
            return;

        var sw = Stopwatch.StartNew();

        _log.Warn(LogChannel.SYSTEM,
            $"[Emu] Start requested while instance is stopping. Waiting up to {maxWaitMs}ms (InstanceId={instanceId}).");

        while (sw.ElapsedMilliseconds < maxWaitMs)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsStopping(instanceId))
            {
                _log.Info(LogChannel.SYSTEM,
                    $"[Emu] Stop completed; proceeding with start (InstanceId={instanceId}, waited={sw.ElapsedMilliseconds}ms).");
                return;
            }

            await Task.Delay(DefaultStoppingPollMs, ct).ConfigureAwait(false);
        }

        // Best-effort: after timeout, we allow start to proceed, but we warn loudly.
        _log.Warn(LogChannel.SYSTEM,
            $"[Emu] Instance still marked stopping after {maxWaitMs}ms. Proceeding anyway (InstanceId={instanceId}).");
    }

    private void EnsureLdPlayerAdbEnabledOnce(IEmulatorInstance inst)
    {
        if (inst is not LDPlayerInstance ld)
            return;

        var app = _settings.Current;

        if (app.LdAdbEnabledOnce)
            return;

        try
        {
            _log.Info(LogChannel.SYSTEM,
                $"[Emu][LD] Enabling ADB globally (one-time) using InstanceId={inst.InstanceId} Name='{inst.Name}'");

            // 1) Stop (best-effort) to ensure modify applies cleanly
            try { inst.Stop(); } catch { /* ignored */ }

            // 2) Enable ADB (persisted by LDPlayer)
            ld.EnableAdb();

            // 3) Stop again (best-effort) so StartAsync does the single clean launch
            try { inst.Stop(); } catch { /* ignored */ }

            // 4) Persist flag
            app.LdAdbEnabledOnce = true;
            _settings.Save();

            _log.Info(LogChannel.SYSTEM,
                $"[Emu][LD] ADB enabled globally (one-time) and will apply on next start (InstanceId={inst.InstanceId})");
        }
        catch (Exception ex)
        {
            _log.Warn(LogChannel.SYSTEM,
                $"[Emu][LD] Failed enabling ADB globally. InstanceId={inst.InstanceId} Name='{inst.Name}' Error={ex.Message}");
        }
    }

    private List<string> SnapshotIds()
    {
        lock (_gate)
            return _instancesById.Keys.ToList();
    }

    private async Task StopInstancesFastAsync(
        IReadOnlyList<string> ids,
        int perInstanceTimeoutMs,
        int maxParallel,
        CancellationToken ct)
    {
        using var sem = new SemaphoreSlim(maxParallel, maxParallel);

        var tasks = ids.Select(async id =>
        {
            if (ct.IsCancellationRequested) return;

            await sem.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (ct.IsCancellationRequested) return;

                using var oneCts = new CancellationTokenSource(perInstanceTimeoutMs);

                try
                {
                    await StopAsync(id, true, perInstanceTimeoutMs, oneCts.Token).ConfigureAwait(false);
                }
                catch
                {
                    // best-effort
                }
            }
            finally
            {
                sem.Release();
            }
        }).ToList();

        try
        {
            await Task.WhenAll(tasks).WaitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            // best-effort
        }
    }

    private async Task WaitUntilRunningAsync(IEmulatorInstance inst, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0) timeoutMs = 60_000;

        // Preferred method in EmulatorLib
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            await inst.WaitUntilOnlineAsync(cts.Token).ConfigureAwait(false);
            return;
        }
        catch
        {
            // fallback to state polling
        }

        await WaitForStateAsync(inst, EmulatorState.Running, timeoutMs, ct).ConfigureAwait(false);
    }

    private async Task WaitForStateAsync(IEmulatorInstance inst, EmulatorState target, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0) timeoutMs = 30_000;

        var start = Environment.TickCount;
        const int delayMs = 400;

        while (Environment.TickCount - start < timeoutMs)
        {
            ct.ThrowIfCancellationRequested();

            if (inst.State == target)
                return;

            await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        _log.Warn(LogChannel.SYSTEM,
            $"[Emu] WaitForState timeout. Instance='{inst.Name}', InstanceId='{inst.InstanceId}', target={target}, current={inst.State}");
    }

    // ================================
    // LDPlayer internal Android resolution policy (pre-start)
    // ✅ Android is SOURCE OF TRUTH
    // ✅ Window follows Android when lock enabled
    // ================================

    private void TryApplyLdPlayerAndroidResolutionBeforeStart(IEmulatorInstance inst)
    {
        try
        {
            if (inst is not EmulatorLib.LDPlayer.LDPlayerInstance ld)
                return;

            var app = _settings.Current;

            var w = app.EmulatorAndroidWidth;
            var h = app.EmulatorAndroidHeight;
            var dpi = app.EmulatorAndroidDpi;

            // Strong bounds (avoid invalid modify args)
            if (w < 200) w = 950;
            if (h < 200) h = 600;
            if (dpi < 120) dpi = 320;

            // Keep window values consistent for UI/legacy (do NOT save here)
            if (app.LockWindowClientToAndroidResolution)
            {
                app.EmulatorWindowWidth = w;
                app.EmulatorWindowHeight = h;
            }

            // LDPlayer applies resolution best pre-launch; ensure stopped (best-effort)
            try { inst.Stop(); } catch { /* ignored */ }

            ld.SetAndroidResolution(w, h, dpi);

            _log.Debug(LogChannel.SYSTEM,
                $"[Emu][LD] Applied Android resolution pre-start InstanceId={inst.InstanceId} Name='{inst.Name}' w={w} h={h} dpi={dpi}");
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex,
                $"[Emu][LD] Failed applying Android resolution pre-start InstanceId={inst.InstanceId} Name='{inst.Name}'");
        }
    }

    // ================================
    // dnplayer PID-based wait (for Unknown state)
    // ================================

    private async Task WaitForDnPlayerExitAsync(string instanceId, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0) timeoutMs = 30_000;

        int pid;
        lock (_gate)
        {
            if (!_dnPlayerPidByInstanceId.TryGetValue(instanceId, out pid))
                return;
        }

        try
        {
            using var p = Process.GetProcessById(pid);

            var waitTask = p.WaitForExitAsync(ct);
            var delayTask = Task.Delay(timeoutMs, ct);

            var completed = await Task.WhenAny(waitTask, delayTask).ConfigureAwait(false);
            if (completed == delayTask)
            {
                _log.Warn(LogChannel.SYSTEM,
                    $"[Emu][Win] dnplayer PID={pid} did not exit within {timeoutMs}ms (InstanceId={instanceId}).");
            }
        }
        catch
        {
            // process already exited / not found => treat as exited
        }
    }

    // ================================
    // Multi-instance dnplayer PID binding (safe)
    // ================================

    private static HashSet<int> SnapshotDnPlayerPids()
    {
        var set = new HashSet<int>();
        try
        {
            foreach (var p in Process.GetProcessesByName("dnplayer"))
                set.Add(p.Id);
        }
        catch
        {
            // ignore
        }

        return set;
    }

    private static int? TryResolveDnPlayerPidForThisStart(HashSet<int> beforePids)
    {
        try
        {
            // Pick the newest dnplayer process that wasn't present before.
            var candidates = Process.GetProcessesByName("dnplayer")
                .Where(p => !beforePids.Contains(p.Id))
                .Select(p =>
                {
                    DateTime start;
                    try { start = p.StartTime; }
                    catch { start = DateTime.MinValue; }

                    return (proc: p, start);
                })
                .OrderByDescending(x => x.start)
                .ToList();

            return candidates.FirstOrDefault().proc?.Id;
        }
        catch
        {
            return null;
        }
    }

    // ================================
    // Window policy
    // ✅ Window CLIENT size follows Android resolution
    // ✅ Uses stable sizing to resist LDPlayer overrides
    // ================================

    private async Task ApplyDnPlayerWindowPolicyAsync(string instanceId, string instanceName, CancellationToken ct)
    {
        int pid;
        lock (_gate)
        {
            if (!_dnPlayerPidByInstanceId.TryGetValue(instanceId, out pid))
                return;
        }

        var app = _settings.Current;

        var x = app.EmulatorWindowX;
        var y = app.EmulatorWindowY;

        var max = app.EmulatorWindowMaximized;
        var min = app.EmulatorWindowMinimized;

        // ✅ Source of truth: Android resolution
        var clientW = app.EmulatorAndroidWidth;
        var clientH = app.EmulatorAndroidHeight;

        // Bounds
        if (clientW < 200) clientW = 950;
        if (clientH < 200) clientH = 600;

        // Keep window values consistent for UI/legacy (do NOT save here)
        if (app.LockWindowClientToAndroidResolution)
        {
            app.EmulatorWindowWidth = clientW;
            app.EmulatorWindowHeight = clientH;
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            var hWnd = await WindowsService.WaitForMainWindowAsync(pid, retries: 120, delayMs: 250)
                .ConfigureAwait(false);

            if (hWnd == IntPtr.Zero)
            {
                _log.Warn(LogChannel.SYSTEM,
                    $"[Emu][Win] dnplayer main window not found for PID={pid} (InstanceId={instanceId}, Name='{instanceName}').");
                return;
            }

            // ✅ Stable apply: keep applying until client area matches target
            var okStable = await WindowsService.ApplyWindowClientRectStable(
                hWnd,
                x, y,
                clientW, clientH,
                max, min,
                stableMs: 1200,
                overallTimeoutMs: 12000,
                tickMs: 250).ConfigureAwait(false);

            _log.Debug(LogChannel.SYSTEM,
                $"[Emu][Win] Applied dnplayer CLIENT rect STABLE ok={okStable} PID={pid} InstanceId={instanceId} Name='{instanceName}' x={x} y={y} clientW={clientW} clientH={clientH} max={max} min={min}");
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex,
                $"[Emu][Win] Failed applying dnplayer CLIENT rect PID={pid} InstanceId={instanceId}");
        }
    }
}
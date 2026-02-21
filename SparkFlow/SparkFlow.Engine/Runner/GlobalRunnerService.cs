/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Runner/GlobalRunnerService.cs
 * Purpose: Core component: GlobalRunnerService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using AdbLib.Abstractions;
using SettingsStore.Interfaces;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Emulator.AutoStart;
using SparkFlow.Abstractions.Services.Emulator.Binding;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using SparkFlow.Domain.Models;
using SparkFlow.Domain.Models.Accounts;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Engine.Runner;

/// <summary>
/// Global runner (ADB-first, emulator-agnostic):
/// - Iterates over Enabled accounts (fixed deterministic order)
/// - (Optional) starts emulator instance (if InstanceId is provided and emulator supports it)
/// - Uses AccountProfile.adbSerial as the Source of Truth for device targeting (Owner ADB)
/// - Waits for device ready via Owner ADB
/// - Launches War and Order
///
/// IMPORTANT:
/// - No list2 parsing.
/// - No "adb connect" retries here.
/// - If a profile has no adbSerial, it is skipped (safe + explicit).
///
/// UPDATED (Pause immediate during account):
/// - PausePointAsync is called between steps + inside polling loops
/// - DeviceReady wait supports PausePoint via AdbClient overload
/// </summary>
public sealed class GlobalRunnerService : IGlobalRunnerService
{
    private readonly IAccountsSelector _accounts;
    private readonly IEmulatorInstanceControlService _emu;
    private readonly IAdbClient _adb;
    private readonly IEmulatorAutoStarter _autoStarter;
    private readonly IProfilesAutoBinder _autoBinder;
    private readonly IRotationManager _rotation;
    private readonly ISettingsAccessor _settings;
    private readonly MLogger _log;

    private static int _sessionCounter;
    private int _sessionId;
    private string _runId = string.Empty;

    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    private GlobalRunnerState _state = GlobalRunnerState.Idle;
    public GlobalRunnerState State => _state;

    public event Action<GlobalRunnerState>? StateChanged;

    // ================= GAME =================

    private const string WarAndOrderPackage = "com.camelgames.superking";

    // Fallback activity (if resolve-activity fails)
    // (Often differs between regions/versions; keep it empty and rely on resolve-activity + monkey)
    private const string WarAndOrderDefaultActivity = "";

    private const int EmulatorStartTimeoutMs = 90_000;
    private const int DeviceReadyTimeoutMs = 60_000;
    private const int LaunchTimeoutMs = 30_000;

    public GlobalRunnerService(
        IAccountsSelector accounts,
        IEmulatorInstanceControlService emu,
        IAdbClient adb,
        IEmulatorAutoStarter autoStarter,
        IProfilesAutoBinder autoBinder,
        IRotationManager rotation,
        ISettingsAccessor settings,
        MLogger logger)
    {
        _accounts = accounts;
        _emu = emu;
        _adb = adb;
        _autoStarter = autoStarter;
        _autoBinder = autoBinder;
        _rotation = rotation;
        _settings = settings;
        _log = logger ?? MLogger.Instance;
    }

    // =========================================================
    // Start / Pause / Resume / Stop
    // =========================================================

    public Task StartAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_runTask is not null && !_runTask.IsCompleted)
                return _runTask;

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _sessionId = Interlocked.Increment(ref _sessionCounter);
            _runId = Guid.NewGuid().ToString("N");

            _rotation.Resume();
            SetState(GlobalRunnerState.Running);

            _runTask = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
            return _runTask;
        }
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (_state != GlobalRunnerState.Running)
                return;

            _rotation.Pause();
            SetState(GlobalRunnerState.Paused);
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (_state != GlobalRunnerState.Paused)
                return;

            _rotation.Resume();
            SetState(GlobalRunnerState.Running);
        }
    }

    public async Task StopAsync()
    {
        Task? runTaskToAwait;

        lock (_gate)
        {
            if (_state == GlobalRunnerState.Stopping)
                return;

            SetState(GlobalRunnerState.Stopping);

            // Ensure we don't stay blocked on Pause gate during stop.
            _rotation.Resume();

            _cts?.Cancel();
            runTaskToAwait = _runTask;
        }

        try
        {
            if (runTaskToAwait is not null)
                await Task.WhenAny(runTaskToAwait, Task.Delay(10_000)).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
        finally
        {
            lock (_gate)
            {
                _cts?.Dispose();
                _cts = null;
                _runTask = null;
                SetState(GlobalRunnerState.Idle);
            }
        }
    }

    // =========================================================
    // Main loop
    // =========================================================

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (true)
            {
                // 0) Background environment preparation (single button UX)
                await _autoStarter.EnsureAnyDeviceReadyAsync(ct).ConfigureAwait(false);

                // Once devices exist, auto-bind any unbound profiles (adbSerial == null)
                await _autoBinder.AutoBindUnboundProfilesAsync(ct).ConfigureAwait(false);

                var enabled = await _accounts.GetEnabledOrderedAsync(ct).ConfigureAwait(false);
                if (enabled.Count == 0)
                {
                    LogRunnerInfo("[Runner] No enabled profiles. Stopping.", profileId: null);
                    break;
                }

                // Refresh emulator instances list once per cycle (optional).
                try { await _emu.RefreshAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }

                LogRunnerInfo($"[Runner] Cycle started. Profiles={enabled.Count} | AutoRestart={_settings.Current.AutoRestartEnabled}", profileId: null);

                foreach (var acc in enabled)
                {
                    // Global pause boundary (between accounts)
                    await _rotation.PausePointAsync(ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();

                    await RunOneAccountAsync(acc, ct).ConfigureAwait(false);
                }

                var autoRestart = _settings.Current.AutoRestartEnabled;
                if (!autoRestart)
                {
                    LogRunnerInfo("[Runner] Cycle finished. Auto-Restart is disabled.", profileId: null);
                    break;
                }

                // Start the next cycle with a fresh RunId for clearer logs.
                _runId = Guid.NewGuid().ToString("N");
                LogRunnerInfo($"[Runner] Auto-Restart: starting a new cycle. RunId={_runId}", profileId: null);
            }

            SetState(GlobalRunnerState.Idle);
        }
        catch (OperationCanceledException)
        {
            SetState(GlobalRunnerState.Idle);
        }
        catch (Exception ex)
        {
            LogRunnerException(ex, "[Runner] Faulted", profileId: null);
            SetState(GlobalRunnerState.Faulted);
        }
    }

    // =========================================================
    // Account execution (UPDATED: Pause is immediate during account)
    // =========================================================

    private Task PausePointAsync(CancellationToken ct) => _rotation.PausePointAsync(ct);

    private async Task RunOneAccountAsync(AccountProfile acc, CancellationToken ct)
    {
        var profileId = acc.Id?.ToString();

        LogRunnerInfo(
            $"[Runner] Account: {acc.Name} | InstanceId={acc.InstanceId} | AdbSerial={acc.AdbSerial}",
            profileId);

        // ✅ Immediate pause/stop boundary (start of account)
        await PausePointAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        // 0) Validate ADB serial binding
        if (string.IsNullOrWhiteSpace(acc.AdbSerial))
        {
            LogRunnerWarn("[Runner] Skipped: adbSerial is missing for this profile.", profileId);
            return;
        }

        var serial = acc.AdbSerial!.Trim();

        // ✅ Pause boundary before emulator step
        await PausePointAsync(ct).ConfigureAwait(false);

        // 1) (Optional) Start emulator instance (InstanceId is treated as string)
        var instanceId = GetInstanceIdString(acc);
        if (IsInstanceIdEnabled(instanceId))
        {
            try
            {
                await _emu.StartAsync(instanceId, waitUntilRunning: false, timeoutMs: EmulatorStartTimeoutMs, ct: ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogRunnerWarn($"[Runner] Emulator start failed: {ex.Message}", profileId);
                // Continue: user may have started emulator manually.
            }
        }

        // ✅ Pause boundary after emulator step
        await PausePointAsync(ct).ConfigureAwait(false);

        // 2) Wait for device ready (UPDATED: supports PausePoint inside polling)
        var ready = await WaitForDeviceReadyAsync(serial, profileId, ct).ConfigureAwait(false);
        if (!ready)
            return;

        // ✅ Pause boundary after ready
        await PausePointAsync(ct).ConfigureAwait(false);

        // 3) Launch the game (UPDATED: PausePoint inside verification polling)
        var launched = await LaunchWarAndOrderAsync(serial, profileId, ct).ConfigureAwait(false);
        if (!launched)
            return;

        // ✅ Pause boundary after launch
        await PausePointAsync(ct).ConfigureAwait(false);

        LogRunnerInfo("[Runner] Device + Game are running ✅", profileId);
    }

    private async Task<bool> WaitForDeviceReadyAsync(string serial, string? profileId, CancellationToken ct)
    {
        LogAdbInfo($"[ADB] Waiting for device ready: {serial}", profileId);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DeviceReadyTimeoutMs);

            // ✅ NEW overload: pause is respected during device polling
            await _adb.WaitForDeviceReadyAsync(serial, PausePointAsync, cts.Token).ConfigureAwait(false);

            LogAdbInfo("[ADB] Device ready ✅", profileId);
            return true;
        }
        catch (OperationCanceledException)
        {
            if (ct.IsCancellationRequested) throw;

            LogAdbWarn("[ADB] Device did not become ready in time.", profileId);
            return false;
        }
        catch (Exception ex)
        {
            LogAdbWarn($"[ADB] Device ready check failed: {ex.Message}", profileId);
            return false;
        }
    }

    private async Task<bool> LaunchWarAndOrderAsync(string serial, string? profileId, CancellationToken ct)
    {
        LogGameInfo("[Game] Launching War and Order...", profileId);

        // ✅ Pause boundary before shell/package check
        await PausePointAsync(ct).ConfigureAwait(false);

        // 1) Ensure package exists
        var packages = await SafeShellAsync(serial, $"pm list packages {WarAndOrderPackage}", 12_000, profileId, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(packages) ||
            !packages.Contains(WarAndOrderPackage, StringComparison.OrdinalIgnoreCase))
        {
            LogGameWarn("[Game] Package not installed on this device.", profileId);
            return false;
        }

        // ✅ Pause boundary before monkey
        await PausePointAsync(ct).ConfigureAwait(false);

        // 2) Try monkey first
        try
        {
            await _adb.StartPackageMonkeyAsync(serial, WarAndOrderPackage, timeoutMs: LaunchTimeoutMs, ct: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogGameWarn($"[Game] monkey failed: {ex.Message}", profileId);
        }

        // ✅ Pause boundary after monkey
        await PausePointAsync(ct).ConfigureAwait(false);

        // 3) If not running, resolve main activity and StartActivity
        if (!await _adb.IsPackageRunningAsync(serial, WarAndOrderPackage, timeoutMs: 8_000, ct: ct).ConfigureAwait(false))
        {
            await PausePointAsync(ct).ConfigureAwait(false);

            var resolved = await SafeShellAsync(serial, $"cmd package resolve-activity --brief {WarAndOrderPackage}", 12_000, profileId, ct)
                .ConfigureAwait(false);

            var component = ExtractActivityComponent(resolved);

            if (string.IsNullOrWhiteSpace(component))
                component = WarAndOrderDefaultActivity;

            await PausePointAsync(ct).ConfigureAwait(false);

            try
            {
                await _adb.StartActivityAsync(serial, component, timeoutMs: LaunchTimeoutMs, ct: ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogGameWarn($"[Game] StartActivity failed: {ex.Message}", profileId);
            }
        }

        // 4) Verify running (poll a bit) - UPDATED: pause respected inside loop
        var ok = await WaitUntilAsync(
            predicate: () =>
            {
                ct.ThrowIfCancellationRequested();
                return _adb.IsPackageRunningAsync(serial, WarAndOrderPackage, timeoutMs: 6_000, ct: ct);
            },
            pausePointAsync: PausePointAsync,
            timeoutMs: 20_000,
            ct: ct).ConfigureAwait(false);

        if (!ok)
        {
            var top = string.Empty;
            try { top = await _adb.GetTopActivityAsync(serial, timeoutMs: 12_000, ct: ct).ConfigureAwait(false); } catch { /* ignore */ }

            LogGameWarn($"[Game] Launch verification failed. TopActivity: {TrimForLog(top)}", profileId);
            return false;
        }

        LogGameInfo("[Game] Started ✅", profileId);
        return true;
    }

    private async Task<string> SafeShellAsync(string serial, string cmd, int timeoutMs, string? profileId, CancellationToken ct)
    {
        try
        {
            // ✅ Pause boundary before each shell call (fast responsiveness)
            await PausePointAsync(ct).ConfigureAwait(false);

            return await _adb.ShellAsync(serial, cmd, timeoutMs, ct).ConfigureAwait(false) ?? string.Empty;
        }
        catch (Exception ex)
        {
            LogRunnerWarn($"[Diag] Shell failed: '{cmd}' => {ex.Message}", profileId);
            return string.Empty;
        }
    }

    private static string ExtractActivityComponent(string resolveOutput)
    {
        if (string.IsNullOrWhiteSpace(resolveOutput))
            return string.Empty;

        var lines = resolveOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Contains('/') && !x.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return lines.Count > 0 ? lines[0] : string.Empty;
    }

    // UPDATED: pausePointAsync inside polling loop
    private static async Task<bool> WaitUntilAsync(
        Func<Task<bool>> predicate,
        Func<CancellationToken, Task> pausePointAsync,
        int timeoutMs,
        CancellationToken ct)
    {
        var start = Environment.TickCount;

        while (Environment.TickCount - start < timeoutMs)
        {
            ct.ThrowIfCancellationRequested();
            await pausePointAsync(ct).ConfigureAwait(false);

            if (await predicate().ConfigureAwait(false))
                return true;

            await pausePointAsync(ct).ConfigureAwait(false);
            await Task.Delay(500, ct).ConfigureAwait(false);
        }

        return false;
    }

    private static string TrimForLog(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        return s.Length <= 350 ? s : s.Substring(0, 350) + "...";
    }

    private void SetState(GlobalRunnerState state)
    {
        _state = state;
        StateChanged?.Invoke(state);

        LogRunnerInfo($"[Runner] State -> {state}", profileId: null);
    }

    // =========================================================
    // InstanceId handling (NO assumptions about int/string)
    // =========================================================

    private static string GetInstanceIdString(AccountProfile acc)
        => acc.InstanceId?.ToString()?.Trim() ?? string.Empty;

    private static bool IsInstanceIdEnabled(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return false;

        // treat "-1" / "none" / "null" as disabled safely
        if (string.Equals(instanceId, "-1", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(instanceId, "none", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(instanceId, "null", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    // =========================================================
    // Logging helpers (compatible with your MLogger)
    // =========================================================

    private void LogRunnerInfo(string msg, string? profileId)
        => _log.Log(LogComponent.Runner, LogChannel.SYSTEM, LogLevel.INFO, msg, _sessionId, _runId, profileId);

    private void LogRunnerWarn(string msg, string? profileId)
        => _log.Log(LogComponent.Runner, LogChannel.SYSTEM, LogLevel.WARNING, msg, _sessionId, _runId, profileId);

    private void LogRunnerException(Exception ex, string context, string? profileId)
        => _log.Exception(LogComponent.Runner, LogChannel.SYSTEM, ex, context, _sessionId, _runId, profileId);

    private void LogAdbInfo(string msg, string? profileId)
        => _log.Log(LogComponent.Adb, LogChannel.SYSTEM, LogLevel.INFO, msg, _sessionId, _runId, profileId);

    private void LogAdbWarn(string msg, string? profileId)
        => _log.Log(LogComponent.Adb, LogChannel.SYSTEM, LogLevel.WARNING, msg, _sessionId, _runId, profileId);

    private void LogGameInfo(string msg, string? profileId)
        => _log.Log(LogComponent.Game, LogChannel.GAME, LogLevel.INFO, msg, _sessionId, _runId, profileId);

    private void LogGameWarn(string msg, string? profileId)
        => _log.Log(LogComponent.Game, LogChannel.GAME, LogLevel.WARNING, msg, _sessionId, _runId, profileId);
}
using System.Diagnostics;
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
using SparkFlow.Domain.Models.Runner;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Engine.Runner;

public sealed class GlobalRunnerService : IGlobalRunnerService
{
    private readonly IAccountsSelector _accounts;
    private readonly IEmulatorInstanceControlService _emu;
    private readonly IAdbClient _adb;
    private readonly IEmulatorAutoStarter _autoStarter;
    private readonly IProfilesAutoBinder _autoBinder;
    private readonly IRotationManager _rotation;
    private readonly ISettingsAccessor _settings;
    private readonly IAccountScheduler _scheduler;
    private readonly IAccountRuntimeStore _runtime;
    private readonly IRunnerMetrics _metrics;
    private readonly IExecutionPolicyEngine _policy;
    private readonly MLogger _log;

    private static int _sessionCounter;
    private int _sessionId;
    private string _runId = string.Empty;

    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    private volatile GlobalRunnerState _state = GlobalRunnerState.Idle;
    public GlobalRunnerState State => _state;

    public event Action<GlobalRunnerState>? StateChanged;

    // ================= GAME =================
    private const string WarAndOrderPackage = "com.camelgames.superking";

    private const int EmulatorStartTimeoutMs = 90_000;
    private const int DeviceReadyTimeoutMs = 60_000;
    private const int LaunchTimeoutMs = 30_000;

    // Loop behavior
    private const int IdleDelayMs = 1200;
    private const int NoEligibleDelayMs = 900;

    public GlobalRunnerService(
        IAccountsSelector accounts,
        IEmulatorInstanceControlService emu,
        IAdbClient adb,
        IEmulatorAutoStarter autoStarter,
        IProfilesAutoBinder autoBinder,
        IRotationManager rotation,
        ISettingsAccessor settings,
        IAccountScheduler scheduler,
        IAccountRuntimeStore runtime,
        IRunnerMetrics metrics,
        IExecutionPolicyEngine policy,
        MLogger logger)
    {
        _accounts = accounts;
        _emu = emu;
        _adb = adb;
        _autoStarter = autoStarter;
        _autoBinder = autoBinder;
        _rotation = rotation;
        _settings = settings;
        _scheduler = scheduler;
        _runtime = runtime;
        _metrics = metrics;
        _policy = policy;
        _log = logger ?? MLogger.Instance;
    }

    // =========================================================
    // Start / Pause / Resume / Stop
    // =========================================================

    public Task StartAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_runTask is { IsCompleted: false })
                return _runTask;

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            _sessionId = Interlocked.Increment(ref _sessionCounter);
            _runId = Guid.NewGuid().ToString("N");

            _rotation.Resume();
            ChangeState(GlobalRunnerState.Running);

            _runTask = Task.Run(() => RunAsync(_cts.Token));
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
            ChangeState(GlobalRunnerState.Paused);
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (_state != GlobalRunnerState.Paused)
                return;

            _rotation.Resume();
            ChangeState(GlobalRunnerState.Running);
        }
    }

    public async Task StopAsync()
    {
        Task? runTaskToAwait;

        lock (_gate)
        {
            if (_state == GlobalRunnerState.Stopping)
                return;

            ChangeState(GlobalRunnerState.Stopping);

            _rotation.Resume();
            _cts?.Cancel();
            runTaskToAwait = _runTask;
        }

        if (runTaskToAwait is not null)
            await Task.WhenAny(runTaskToAwait, Task.Delay(10_000)).ConfigureAwait(false);

        lock (_gate)
        {
            _cts?.Dispose();
            _cts = null;
            _runTask = null;
            ChangeState(GlobalRunnerState.Idle);
        }
    }

    // =========================================================
    // Main loop (Scheduler-driven)
    // =========================================================

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await _rotation.PausePointAsync(ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                var nowUtc = DateTimeOffset.UtcNow;

                await _autoStarter.EnsureAnyDeviceReadyAsync(ct).ConfigureAwait(false);
                await _autoBinder.AutoBindUnboundProfilesAsync(ct).ConfigureAwait(false);

                var enabled = await _accounts.GetEnabledOrderedAsync(ct).ConfigureAwait(false);
                if (enabled.Count == 0)
                {
                    LogRunnerInfo("[Runner] No enabled profiles. Going idle.", null);
                    await Task.Delay(IdleDelayMs, ct).ConfigureAwait(false);
                    continue;
                }

                try { await _emu.RefreshAsync(ct).ConfigureAwait(false); } catch { }

                var maxBatch = 1; // sequential for now
                var batch = _scheduler.SelectNextBatch(enabled, nowUtc, maxBatch);

                if (batch.Count == 0)
                {
                    _metrics.Inc("runner.no_eligible");
                    await Task.Delay(NoEligibleDelayMs, ct).ConfigureAwait(false);
                    continue;
                }

                foreach (var acc in batch)
                {
                    await _rotation.PausePointAsync(ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();

                    await ExecuteAccountEnterpriseAsync(acc, ct).ConfigureAwait(false);
                }

                if (!_settings.Current.AutoRestartEnabled)
                {
                    LogRunnerInfo("[Runner] Auto-Restart disabled. Stopping.", null);
                    break;
                }
            }

            ChangeState(GlobalRunnerState.Idle);
        }
        catch (OperationCanceledException)
        {
            ChangeState(GlobalRunnerState.Idle);
        }
        catch (Exception ex)
        {
            LogRunnerException(ex, "[Runner] Faulted", null);
            ChangeState(GlobalRunnerState.Faulted);
        }
    }

    // =========================================================
    // Enterprise account execution wrapper (Stage-2: policy-driven)
    // =========================================================

    private async Task ExecuteAccountEnterpriseAsync(AccountProfile acc, CancellationToken ct)
    {
        var profileId = acc.Id?.ToString() ?? "";
        var now = DateTimeOffset.UtcNow;

        // 1) Mark start atomically
        _runtime.Upsert(profileId, st =>
        {
            st.LastStartedUtc = now;
            st.UpdatedAtUtc = now;
            return st;
        });

        var sw = Stopwatch.StartNew();

        // Important: Policy must decide next-run/disable/circuit decisions.
        var context = new PolicyContext(
            Profile: acc,
            Runtime: _runtime.GetOrCreate(profileId),
            NowUtc: now);

        var outcome = await _policy.ExecuteAsync(
            context,
            runOnceAsync: innerCt => RunOneAccountAsync(acc, innerCt),
            ct).ConfigureAwait(false);

        sw.Stop();

        // 2) Apply policy decision atomically
        _runtime.Upsert(profileId, st =>
        {
            var finishedAt = DateTimeOffset.UtcNow;

            st.LastFinishedUtc = finishedAt;
            st.UpdatedAtUtc = finishedAt;

            if (outcome.Success)
            {
                // Domain method: resets failures + clears last failure fields
                st.MarkSuccess(finishedAt);
            }
            else
            {
                var failure = outcome.Failure!;

                // Domain method: increments failures + sets last failure fields (enum-safe)
                st.MarkFailure(finishedAt, failure);

                // Optional disable window (enterprise safety)
                if (outcome.Decision.DisableFor.HasValue)
                    st.DisableFor(outcome.Decision.DisableFor.Value, finishedAt, failure);
            }

            // Policy is the source of truth for scheduling
            st.NextRunAtUtc = outcome.Decision.NextRunAtUtc;

            return st;
        });

        // 3) Metrics
        if (outcome.Success)
            _metrics.Inc("account.success");
        else
            _metrics.Inc($"account.failure.{outcome.Failure!.Type}");

        _metrics.ObserveMs("account.duration_ms", sw.ElapsedMilliseconds);
    }

    // =========================================================
    // Account execution (ADB-first, pause-responsiveness)
    // =========================================================

    private Task PausePointAsync(CancellationToken ct) => _rotation.PausePointAsync(ct);

    private async Task RunOneAccountAsync(AccountProfile acc, CancellationToken ct)
    {
        var profileId = acc.Id?.ToString();

        LogRunnerInfo(
            $"[Runner] Account: {acc.Name} | InstanceId={acc.InstanceId} | AdbSerial={acc.AdbSerial}",
            profileId);

        await PausePointAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(acc.AdbSerial))
            throw new RunnerClassifiedException(new RunnerFailure(RunnerFailureType.Skipped_NoSerial, "adbSerial missing"));

        var serial = acc.AdbSerial.Trim();

        await PausePointAsync(ct).ConfigureAwait(false);

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
                throw new RunnerClassifiedException(new RunnerFailure(RunnerFailureType.EmulatorStartFailed, ex.Message));
            }
        }

        await PausePointAsync(ct).ConfigureAwait(false);

        var ready = await WaitForDeviceReadyAsync(serial, profileId, ct).ConfigureAwait(false);
        if (!ready)
            throw new RunnerClassifiedException(new RunnerFailure(RunnerFailureType.DeviceReadyTimeout, "device not ready in time"));

        await PausePointAsync(ct).ConfigureAwait(false);

        var launched = await LaunchWarAndOrderAsync(serial, profileId, ct).ConfigureAwait(false);
        if (!launched)
            throw new RunnerClassifiedException(new RunnerFailure(RunnerFailureType.GameLaunchFailed, "launch verification failed"));

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

        await PausePointAsync(ct).ConfigureAwait(false);

        var packages = await SafeShellAsync(serial, $"pm list packages {WarAndOrderPackage}", 12_000, profileId, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(packages) ||
            !packages.Contains(WarAndOrderPackage, StringComparison.OrdinalIgnoreCase))
        {
            LogGameWarn("[Game] Package not installed on this device.", profileId);
            throw new RunnerClassifiedException(new RunnerFailure(RunnerFailureType.GameNotInstalled, "package missing"));
        }

        await PausePointAsync(ct).ConfigureAwait(false);

        try
        {
            await _adb.StartPackageMonkeyAsync(serial, WarAndOrderPackage, timeoutMs: LaunchTimeoutMs, ct: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogGameWarn($"[Game] monkey failed: {ex.Message}", profileId);
        }

        await PausePointAsync(ct).ConfigureAwait(false);

        if (!await _adb.IsPackageRunningAsync(serial, WarAndOrderPackage, timeoutMs: 8_000, ct: ct).ConfigureAwait(false))
        {
            await PausePointAsync(ct).ConfigureAwait(false);

            var resolved = await SafeShellAsync(serial, $"cmd package resolve-activity --brief {WarAndOrderPackage}", 12_000, profileId, ct)
                .ConfigureAwait(false);

            var component = ExtractActivityComponent(resolved);

            if (!string.IsNullOrWhiteSpace(component))
            {
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
        }

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
            try { top = await _adb.GetTopActivityAsync(serial, timeoutMs: 12_000, ct: ct).ConfigureAwait(false); } catch { }

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

        foreach (var raw in resolveOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.StartsWith("name=", StringComparison.OrdinalIgnoreCase)) continue;
            if (!line.Contains('/')) continue;
            if (line.Count(c => c == '/') == 1)
                return line;
        }

        return string.Empty;
    }

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

    private static string GetInstanceIdString(AccountProfile acc)
        => acc.InstanceId?.ToString()?.Trim() ?? string.Empty;

    private static bool IsInstanceIdEnabled(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return false;

        if (string.Equals(instanceId, "-1", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(instanceId, "none", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(instanceId, "null", StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    private void ChangeState(GlobalRunnerState state)
    {
        _state = state;
        var handler = StateChanged;
        handler?.Invoke(state);

        LogRunnerInfo($"[Runner] State -> {state}", null);
    }

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
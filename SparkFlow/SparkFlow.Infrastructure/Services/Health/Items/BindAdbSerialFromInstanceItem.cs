/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/BindAdbSerialFromInstanceItem.cs
 * Purpose: HealthCheck item: bind ADB serial by opening emulator instance (InstanceId).
 * Notes:
 *  - Opens instance -> detects ADB device -> stores serial into profile -> closes instance.
 *  - Does NOT touch account Active/Toggle.
 *  - Best-effort and SAFE:
 *      - Prefer binding EXPECTED emulator serial for this InstanceId if computable.
 *      - Otherwise:
 *          - Prefer exactly ONE "new device" compared to before snapshot.
 *          - Otherwise, prefer exactly ONE "bindable" device (ready + not already used by any profile).
 *  - Uses dedicated Health/AutoFix timeouts (fast) from AppSettings:
 *      - TimeoutAutoBindSerialSec
 *      - BootingDelayAutoBindSec
 *
 * NEW POLICY (Fast stop + background confirm):
 *  ✅ We send a FAST stop request (waitUntilStopped=false) to keep FixAll/UI responsive.
 *  ✅ In background, we confirm the instance fully stopped (waitUntilStopped=true).
 *  ✅ EmulatorInstanceControlService has a "stopping" flag to prevent StartAsync racing
 *     while shutdown is still in progress.
 * ============================================================================ */

using System.Diagnostics;
using AdbLib.Abstractions;
using AdbLib.Models;
using SettingsStore.Interfaces;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using SparkFlow.Abstractions.Services.Health.Abstractions;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class BindAdbSerialFromInstanceItem : IHealthCheckItem
{
    private readonly IProfilesStore _profiles;
    private readonly IEmulatorInstanceControlService _emu;
    private readonly IAdbClient _adb;
    private readonly ISettingsAccessor _settings;
    private readonly MLogger _log;

    // Fallbacks (if settings values are zero/invalid)
    private const int DefaultEmuStartTimeoutMs = 90_000;

    // IMPORTANT: Health FixAll must be FAST, not full session boot windows.
    private const int DefaultAutoBindTimeoutSec = 45;
    private const int DefaultAutoBindBootDelaySec = 0;

    // Polling cadence
    private const int PollDelayMs = 400;

    // If ADB returns 0 devices for a while, we try adb connect, then kick server once (best-effort)
    private const int ZeroDevicesKickAfterMs = 2500;

    public BindAdbSerialFromInstanceItem(
        IProfilesStore profiles,
        IEmulatorInstanceControlService emu,
        IAdbClient adb,
        ISettingsAccessor settings,
        MLogger logger)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _emu = emu ?? throw new ArgumentNullException(nameof(emu));
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = logger ?? MLogger.Instance;
    }

    public HealthCheckItemId Id => HealthCheckItemId.BindAdbSerialFromInstance;
    public string Title => "Bind ADB Serial (InstanceId)";

    public async Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var profile = await _profiles.GetByIdAsync(ctx.ProfileId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.profile_missing",
                Title = "Profile missing",
                Details = $"Profile '{ctx.ProfileId}' not found in ProfilesStore.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.None
            };
        }

        if (!string.IsNullOrWhiteSpace(profile.AdbSerial))
            return null;

        if (string.IsNullOrWhiteSpace(profile.InstanceId))
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.instance_missing",
                Title = "InstanceId is missing",
                Details = "This profile has no InstanceId. Cannot auto-bind ADB serial.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Manual,
                ManualSteps =
                    "1) Open the emulator instance manually.\n" +
                    "2) Ensure the device appears in 'adb devices' as state 'device'.\n" +
                    "3) Re-run Health Check (Fix All)."
            };
        }

        return new HealthIssue
        {
            Code = $"health.{Id}.serial_missing",
            Title = "No ADB serial bound",
            Details = $"Profile has InstanceId='{profile.InstanceId}' but AdbSerial is empty. Auto-bind is available.",
            Severity = HealthIssueSeverity.Blocker,
            FixKind = HealthFixKind.Auto
        };
    }

    public async Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var profile = await _profiles.GetByIdAsync(ctx.ProfileId, ct).ConfigureAwait(false);
        if (profile is null)
        {
            _log.Warn(LogChannel.SYSTEM, $"[Health][Bind] Profile missing (ProfileId='{ctx.ProfileId}').");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(profile.AdbSerial))
            return false;

        if (string.IsNullOrWhiteSpace(profile.InstanceId))
        {
            _log.Warn(LogChannel.SYSTEM, $"[Health][Bind] InstanceId missing (ProfileId='{profile.Id}').");
            return false;
        }

        var instanceId = profile.InstanceId.Trim();

        // Read timeouts from settings.Current (FAST dedicated bind timeouts)
        var s = _settings.Current;

        var startTimeoutMs = (s.TimeoutEmulatorStart > 0)
            ? s.TimeoutEmulatorStart * 1000
            : DefaultEmuStartTimeoutMs;

        var bindTimeoutSec = (s.TimeoutAutoBindSerialSec > 0)
            ? s.TimeoutAutoBindSerialSec
            : DefaultAutoBindTimeoutSec;

        var bindBootDelaySec = (s.BootingDelayAutoBindSec >= 0)
            ? s.BootingDelayAutoBindSec
            : DefaultAutoBindBootDelaySec;

        // Total budget for the whole bind flow (used to compute dynamic stop timeout)
        var totalBudgetMs = Math.Max(6000, bindTimeoutSec * 1000);

        // Dynamic stop timeout (computed AFTER actual start duration, but must exist for finally)
        var stopTimeoutMs = 6000;

        // Snapshot devices before (ASYNC)
        var before = await SnapshotDevicesAsync(ct).ConfigureAwait(false);

        // Expected serials:
        // - Legacy emulator serial (emulator-5554, emulator-5556, ...)
        var expectedSerialLegacy = TryComputeExpectedSerial(instanceId);

        // - TCP serial (127.0.0.1:5555, 127.0.0.1:5557, ...)
        var expectedTcpPortFallback = TryComputeExpectedTcpPort(instanceId);
        var expectedSerialTcp = TryComputeExpectedSerialTcp(instanceId, expectedTcpPortFallback);

        _log.Info(LogChannel.SYSTEM,
            $"[Health][Bind] Start Fix | ProfileId='{profile.Id}', InstanceId='{instanceId}', " +
            $"ExpectedTcp={(expectedSerialTcp is null ? "n/a" : $"'{expectedSerialTcp}'")}, " +
            $"ExpectedLegacy={(expectedSerialLegacy is null ? "n/a" : $"'{expectedSerialLegacy}'")}, " +
            $"BeforeDevices={before.Count}, StartTimeoutMs={startTimeoutMs}, BindTimeoutSec={bindTimeoutSec}, BootDelaySec={bindBootDelaySec}, TotalBudgetMs={totalBudgetMs}");

        // Preload all bound serials so we can avoid collisions when choosing fallbacks
        var allProfiles = await _profiles.LoadAllAsync(ct).ConfigureAwait(false);

        var boundSerials = new HashSet<string>(
            allProfiles
                .Select(p => (p.AdbSerial ?? "").Trim())
                .Where(x => x.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        string? detectedSerial = null;

        // Will try to resolve instance port from emulator cache and attempt adb connect.
        int? instancePort = null;

        // Actual timings
        var startElapsedMs = 0L;

        try
        {
            // Start instance (wait running) + measure ACTUAL wait time
            var swStart = Stopwatch.StartNew();

            await _emu.StartAsync(instanceId, waitUntilRunning: true, timeoutMs: startTimeoutMs, ct: ct)
                .ConfigureAwait(false);

            swStart.Stop();
            startElapsedMs = swStart.ElapsedMilliseconds;

            _log.Info(LogChannel.SYSTEM,
                $"[Health][Bind] StartAsync -> Running in {swStart.Elapsed.TotalSeconds:0.0}s ({startElapsedMs} ms) " +
                $"(InstanceId='{instanceId}', TimeoutMs={startTimeoutMs}).");

            // Compute dynamic stop timeout from remaining budget
            var remainingMs = (int)Math.Max(0, totalBudgetMs - startElapsedMs);

            stopTimeoutMs = Math.Max(
                6000,
                Math.Min(startTimeoutMs, remainingMs));

            _log.Info(LogChannel.SYSTEM,
                $"[Health][Bind] StopTimeoutMs(dynamic)={stopTimeoutMs} (RemainingMs={remainingMs}, TotalBudgetMs={totalBudgetMs}).");

            if (bindBootDelaySec > 0)
            {
                _log.Info(LogChannel.SYSTEM,
                    $"[Health][Bind] Boot delay: {bindBootDelaySec}s (InstanceId='{instanceId}').");

                await Task.Delay(TimeSpan.FromSeconds(bindBootDelaySec), ct).ConfigureAwait(false);
            }

            // Try to resolve instance port (preferred) and attempt adb connect once.
            try
            {
                await _emu.RefreshAsync(ct).ConfigureAwait(false);
                var instObj = _emu.TryGetById(instanceId);
                instancePort = instObj?.AdbPort;
            }
            catch
            {
                // ignore
            }

            var portToTry = instancePort ?? expectedTcpPortFallback;
            if (portToTry.HasValue)
                await TryAdbConnectBestEffortAsync(portToTry.Value, ct).ConfigureAwait(false);

            var deadline = DateTimeOffset.UtcNow.AddSeconds(bindTimeoutSec);

            // ADB kick tracking
            var firstZeroDevicesAt = (DateTimeOffset?)null;
            var didKickAdb = false;

            while (DateTimeOffset.UtcNow < deadline)
            {
                ct.ThrowIfCancellationRequested();

                var now = await SnapshotDevicesAsync(ct).ConfigureAwait(false);

                // If ADB returns 0 devices for a bit, try adb connect then kick server ONCE (best-effort)
                if (now.Count == 0)
                {
                    firstZeroDevicesAt ??= DateTimeOffset.UtcNow;

                    if (!didKickAdb &&
                        (DateTimeOffset.UtcNow - firstZeroDevicesAt.Value).TotalMilliseconds >= ZeroDevicesKickAfterMs)
                    {
                        didKickAdb = true;

                        _log.Warn(LogChannel.SYSTEM,
                            $"[Health][Bind] ADB devices=0 for {(int)(DateTimeOffset.UtcNow - firstZeroDevicesAt.Value).TotalMilliseconds}ms. Trying adb connect, then (if needed) kick ADB server.");

                        var p2 = instancePort ?? expectedTcpPortFallback;

                        var didConnect = false;
                        if (p2.HasValue)
                            didConnect = await TryAdbConnectBestEffortAsync(p2.Value, ct).ConfigureAwait(false);

                        if (!didConnect)
                        {
                            await RestartAdbServerBestEffortAsync(ct).ConfigureAwait(false);

                            // After restart, try connect again once
                            if (p2.HasValue)
                                await TryAdbConnectBestEffortAsync(p2.Value, ct).ConfigureAwait(false);
                        }

                        await Task.Delay(800, ct).ConfigureAwait(false);
                        continue;
                    }
                }
                else
                {
                    firstZeroDevicesAt = null;
                }

                // 1) BEST: expected serial becomes state=device (TCP first, then legacy)
                foreach (var expected in new[] { expectedSerialTcp, expectedSerialLegacy })
                {
                    if (string.IsNullOrWhiteSpace(expected))
                        continue;

                    var match = now.FirstOrDefault(d =>
                        string.Equals((d.Serial ?? "").Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                    {
                        if (string.Equals(match.State, "device", StringComparison.OrdinalIgnoreCase))
                        {
                            detectedSerial = expected.Trim();
                            break;
                        }

                        // present but not ready yet (offline/unauthorized/etc.)
                        await Task.Delay(PollDelayMs, ct).ConfigureAwait(false);
                    }
                }

                if (!string.IsNullOrWhiteSpace(detectedSerial))
                    break;

                // 2) Prefer NEW devices (compared to before)
                var newOnes = GetNewDevices(before, now);

                if (newOnes.Count == 1)
                {
                    var candidate = newOnes[0];
                    if (string.Equals(candidate.State, "device", StringComparison.OrdinalIgnoreCase))
                    {
                        var candSerial = (candidate.Serial ?? "").Trim();
                        if (candSerial.Length > 0 && !boundSerials.Contains(candSerial))
                        {
                            detectedSerial = candSerial;
                            break;
                        }
                    }
                }
                else if (newOnes.Count > 1)
                {
                    _log.Warn(LogChannel.SYSTEM,
                        $"[Health][Bind] Ambiguous: multiple new devices appeared ({newOnes.Count}). Will not auto-bind.");
                    detectedSerial = null;
                    break;
                }

                // 3) Fallback: exactly ONE unbound ready device
                var bindable = now
                    .Where(d =>
                        !string.IsNullOrWhiteSpace(d.Serial) &&
                        string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase) &&
                        !boundSerials.Contains(d.Serial!.Trim()))
                    .Select(d => d.Serial!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (bindable.Count == 1)
                {
                    detectedSerial = bindable[0];
                    break;
                }

                await Task.Delay(PollDelayMs, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(detectedSerial))
            {
                // Ensure device is actually ready (extra safety)
                try
                {
                    await _adb.WaitForDeviceReadyAsync(detectedSerial, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Exception(LogChannel.SYSTEM, ex,
                        $"[Health][Bind] WaitForDeviceReady failed for Serial='{detectedSerial}' (ProfileId='{profile.Id}', InstanceId='{instanceId}').");
                    return false;
                }

                profile.AdbSerial = detectedSerial.Trim();
                await _profiles.SaveAsync(profile, ct).ConfigureAwait(false);

                _log.Info(LogChannel.SYSTEM,
                    $"[Health][Bind] Bound AdbSerial='{profile.AdbSerial}' to ProfileId='{profile.Id}' (InstanceId='{instanceId}').");

                return true;
            }

            _log.Warn(LogChannel.SYSTEM,
                $"[Health][Bind] Failed to detect a single ready device within timeout (ProfileId='{profile.Id}', InstanceId='{instanceId}', TimeoutSec={bindTimeoutSec}).");

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex,
                $"[Health][Bind] Failed during bind flow (InstanceId='{instanceId}', ProfileId='{profile.Id}').");
            return false;
        }
        finally
        {
            // ============================================================
            // NEW: Two-phase stop
            // 1) FAST stop request (wait=false) to keep UI/FixAll responsive.
            // 2) BACKGROUND confirm stop (wait=true) to ensure instance fully exits.
            // EmulatorInstanceControlService will prevent StartAsync racing while stopping.
            // ============================================================

            // Phase 1: Fast stop request
            try
            {
                var swFast = Stopwatch.StartNew();

                await _emu.StopAsync(
                        instanceId,
                        waitUntilStopped: false,
                        timeoutMs: 2500, // small budget: issue stop command only
                        ct: ct)
                    .ConfigureAwait(false);

                swFast.Stop();

                _log.Info(LogChannel.SYSTEM,
                    $"[Health][Bind] Stop FAST request sent in {swFast.Elapsed.TotalSeconds:0.0}s ({swFast.ElapsedMilliseconds} ms) " +
                    $"(InstanceId='{instanceId}', wait=false).");
            }
            catch (Exception ex)
            {
                _log.Exception(LogChannel.SYSTEM, ex,
                    $"[Health][Bind] Stop FAST request failed (InstanceId='{instanceId}').");
            }

            // Phase 2: Background confirm stop (do not block)
            _ = Task.Run(async () =>
            {
                try
                {
                    var swBg = Stopwatch.StartNew();

                    // Give a reasonable budget for graceful shutdown.
                    // Use dynamic stopTimeoutMs, but ensure a minimum.
                    var bgTimeoutMs = Math.Max(12_000, stopTimeoutMs);

                    await _emu.StopAsync(
                            instanceId,
                            waitUntilStopped: true,
                            timeoutMs: bgTimeoutMs,
                            ct: CancellationToken.None) // do not cancel with health check UI
                        .ConfigureAwait(false);

                    swBg.Stop();

                    _log.Info(LogChannel.SYSTEM,
                        $"[Health][Bind] Stop BACKGROUND confirmed in {swBg.Elapsed.TotalSeconds:0.0}s ({swBg.ElapsedMilliseconds} ms) " +
                        $"(InstanceId='{instanceId}', timeoutMs={bgTimeoutMs}, StartElapsedMs={startElapsedMs}).");
                }
                catch (Exception ex)
                {
                    _log.Exception(LogChannel.SYSTEM, ex,
                        $"[Health][Bind] Stop BACKGROUND confirm failed (InstanceId='{instanceId}').");
                }
            });
        }
    }

    private async Task<IReadOnlyList<AdbDevice>> SnapshotDevicesAsync(CancellationToken ct)
    {
        try
        {
            return await _adb.DevicesAsync(15_000, ct).ConfigureAwait(false);
        }
        catch
        {
            return Array.Empty<AdbDevice>();
        }
    }

    private static List<AdbDevice> GetNewDevices(IReadOnlyList<AdbDevice> before, IReadOnlyList<AdbDevice> after)
    {
        var beforeSet = new HashSet<string>(
            before.Select(d => (d.Serial ?? "").Trim()),
            StringComparer.OrdinalIgnoreCase);

        var list = new List<AdbDevice>();

        foreach (var d in after)
        {
            var s = (d.Serial ?? "").Trim();
            if (s.Length == 0) continue;

            if (!beforeSet.Contains(s))
                list.Add(d);
        }

        return list;
    }

    /// <summary>
    /// Legacy emulator TCP serials (common for some Android emulators):
    ///  - instance index 0 -> emulator-5554
    ///  - instance index 1 -> emulator-5556
    ///  - instance index 2 -> emulator-5558
    /// </summary>
    private static string? TryComputeExpectedSerial(string instanceId)
    {
        if (!int.TryParse(instanceId, out var idx)) return null;
        if (idx < 0) return null;

        var port = 5554 + (2 * idx);
        return $"emulator-{port}";
    }

    /// <summary>
    /// LDPlayer (newer) often exposes ADB as TCP endpoints:
    ///  - index 0 -> 127.0.0.1:5555
    ///  - index 1 -> 127.0.0.1:5557
    ///  - index 2 -> 127.0.0.1:5559
    /// </summary>
    private static int? TryComputeExpectedTcpPort(string instanceId)
    {
        if (!int.TryParse(instanceId, out var idx)) return null;
        if (idx < 0) return null;

        return 5555 + (2 * idx);
    }

    private static string? TryComputeExpectedSerialTcp(string instanceId, int? portOverride = null)
    {
        var port = portOverride ?? TryComputeExpectedTcpPort(instanceId);
        if (port is null || port.Value <= 0) return null;
        return $"127.0.0.1:{port.Value}";
    }

    private async Task<bool> TryAdbConnectBestEffortAsync(int port, CancellationToken ct)
    {
        if (port <= 0) return false;

        var endpoints = new[]
        {
            $"127.0.0.1:{port}",
            $"localhost:{port}"
        };

        foreach (var ep in endpoints)
        {
            try
            {
                await _adb.RunRawAsync($"connect {ep}", 12_000, ct).ConfigureAwait(false);
                _log.Debug(LogChannel.SYSTEM, $"[Health][Bind] adb connect attempted: {ep}");
                return true;
            }
            catch (Exception ex)
            {
                _log.Debug(LogChannel.SYSTEM, $"[Health][Bind] adb connect failed: {ep} | {ex.Message}");
            }
        }

        return false;
    }

    private async Task RestartAdbServerBestEffortAsync(CancellationToken ct)
    {
        try { await _adb.KillServerAsync(12_000, ct).ConfigureAwait(false); }
        catch { /* ignore */ }

        try { await _adb.StartServerAsync(20_000, ct).ConfigureAwait(false); }
        catch { /* ignore */ }
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.Core/Services/Emulator/AutoStart/LDPlayerEmulatorAutoStarter.cs
 * Purpose: Ensure emulator is started and at least one ADB device becomes ready.
 * Notes:
 *  - Uses EmulatorLib new API.
 *  - Uses ADB to verify device readiness.
 *  - UPDATED: Now prefers instances required by enabled profiles.
 * ============================================================================ */

using AdbLib.Abstractions;
using EmulatorLib.Abstractions;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Emulator.AutoStart;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Emulator.AutoStart;

public sealed class LDPlayerEmulatorAutoStarter : IEmulatorAutoStarter
{
    private readonly IAdbClient _adb;
    private readonly IEmulator _emulator;
    private readonly IAccountsSelector _accounts; // ✅ NEW
    private readonly MLogger _log;

    private const int WaitForDeviceTimeoutMs = 90_000;
    private const int PollDelayMs = 1500;
    private const int StartOnlineTimeoutMs = 60_000;

    public LDPlayerEmulatorAutoStarter(
        IAdbClient adb,
        IEmulator emulator,
        IAccountsSelector accounts, // ✅ NEW
        MLogger logger)
    {
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
        _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts)); // ✅ NEW
        _log = logger ?? MLogger.Instance;
    }

    public async Task EnsureAnyDeviceReadyAsync(CancellationToken ct = default)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                _log.Error(LogChannel.SYSTEM, "[AutoStart] SparkFlow requires Windows + LDPlayer.");
                return;
            }

            TryEnsureAdbServer();

            if (HasAnyReadyDevice())
            {
                _log.Debug(LogChannel.SYSTEM, "[AutoStart] ADB already has ready devices.");
                return;
            }

            if (!_emulator.IsInstalled)
            {
                _log.Error(LogChannel.SYSTEM, "[AutoStart] LDPlayer is not installed or not detected.");
                return;
            }

            var instances = SafeScanInstances();
            if (instances.Count == 0)
            {
                _log.Error(LogChannel.SYSTEM, "[AutoStart] No LDPlayer instances found.");
                return;
            }

            // ============================================================
            // ✅ NEW LOGIC: Prefer instances required by enabled profiles
            // ============================================================

            var enabledProfiles = await _accounts.GetEnabledOrderedAsync(ct);

            var requiredInstanceIds = enabledProfiles
                .Select(p => p.InstanceId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            IEmulatorInstance? target = null;

            foreach (var id in requiredInstanceIds)
            {
                target = instances.FirstOrDefault(x =>
                    string.Equals(x.InstanceId, id, StringComparison.OrdinalIgnoreCase));

                if (target is not null)
                {
                    _log.Info(LogChannel.SYSTEM,
                        $"[AutoStart] Selecting instance from enabled profiles: {target.Name} (InstanceId={target.InstanceId})");
                    break;
                }
            }

            // ============================================================
            // Fallback to smallest instance if none matched
            // ============================================================

            target ??= instances
                .OrderBy(x => TryParseInt(x.InstanceId, out var n) ? n : int.MaxValue)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .First();

            _log.Info(LogChannel.SYSTEM,
                $"[AutoStart] Starting LDPlayer instance '{target.Name}' (InstanceId={target.InstanceId})");

            await StartInstanceBestEffortAsync(target, ct).ConfigureAwait(false);

            var ok = await WaitUntilAnyReadyDeviceAsync(ct).ConfigureAwait(false);
            if (!ok)
            {
                _log.Warn(LogChannel.SYSTEM, "[AutoStart] No ready ADB devices after starting LDPlayer.");
                return;
            }

            _log.Info(LogChannel.SYSTEM, "[AutoStart] ADB device detected successfully ✅");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex, "[AutoStart] EnsureAnyDeviceReadyAsync failed");
        }
    }

    private IReadOnlyList<IEmulatorInstance> SafeScanInstances()
    {
        try
        {
            return _emulator.ScanInstances().ToList();
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex, "[AutoStart] ScanInstances failed");
            return Array.Empty<IEmulatorInstance>();
        }
    }

    private void TryEnsureAdbServer()
    {
        try
        {
            _adb.StartServer();
        }
        catch
        {
            // best-effort
        }
    }

    private bool HasAnyReadyDevice()
    {
        try
        {
            var devices = _adb.Devices();
            return devices.Any(d =>
                string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> WaitUntilAnyReadyDeviceAsync(CancellationToken ct)
    {
        var start = Environment.TickCount;

        while (Environment.TickCount - start < WaitForDeviceTimeoutMs)
        {
            ct.ThrowIfCancellationRequested();

            if (HasAnyReadyDevice())
                return true;

            await Task.Delay(PollDelayMs, ct).ConfigureAwait(false);
        }

        return false;
    }

    private async Task StartInstanceBestEffortAsync(IEmulatorInstance inst, CancellationToken ct)
    {
        try
        {
            if (inst.State == EmulatorState.Running)
            {
                _log.Debug(LogChannel.SYSTEM, $"[AutoStart] Instance already running: {inst.Name}");
                return;
            }

            inst.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(StartOnlineTimeoutMs);

            try
            {
                await inst.WaitUntilOnlineAsync(cts.Token).ConfigureAwait(false);
            }
            catch
            {
                // ignore (best-effort)
            }
        }
        catch (Exception ex)
        {
            _log.Warn(LogChannel.SYSTEM,
                $"[AutoStart] Failed to start instance '{inst.Name}' (InstanceId={inst.InstanceId}): {ex.Message}");
        }
    }

    private static bool TryParseInt(string s, out int value)
        => int.TryParse(s, out value);
}

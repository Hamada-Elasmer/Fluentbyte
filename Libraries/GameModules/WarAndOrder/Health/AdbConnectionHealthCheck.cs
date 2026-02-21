/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Health/AdbConnectionHealthCheck.cs
 * Purpose: Library component: AdbConnectionHealthCheck.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Threading;
using System.Threading.Tasks;
using GameContracts.Common;
using GameContracts.Health;
using GameModules.WarAndOrder.Ports;

namespace GameModules.WarAndOrder.Health;

public sealed class AdbConnectionHealthCheck : IGameHealthCheck
{
    private readonly IEmulatorController? _emulator;
    private readonly IDeviceAutomation _device;

    public AdbConnectionHealthCheck(IDeviceAutomation device, IEmulatorController? emulator = null)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _emulator = emulator;
    }

    public string CheckId => "adb_connection";

    public async Task<GameHealthIssue?> CheckAsync(GameContext context, CancellationToken ct)
    {
        try
        {
            var deviceId = await ResolveDeviceIdAsync(context, ct).ConfigureAwait(false);
            await _device.WaitForDeviceReadyAsync(deviceId, ct).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return new GameHealthIssue(
                Id: CheckId,
                Severity: GameHealthSeverity.Blocker,
                Title: "ADB connection is not ready",
                Details: ex.Message,
                CanAutoFix: true
            );
        }
    }

    public async Task<bool> FixAsync(GameContext context, CancellationToken ct)
    {
        // TODO:
        // - adb connect best-effort
        // - restart emulator instance
        // - re-resolve device id
        await Task.Delay(100, ct).ConfigureAwait(false);
        return false;
    }

    private async Task<string> ResolveDeviceIdAsync(GameContext context, CancellationToken ct)
    {
        // ✅ First choice: use DeviceId if the platform already provided it (ADB serial).
        if (!string.IsNullOrWhiteSpace(context.DeviceId))
            return context.DeviceId!;

        // ✅ Fallback: resolve via emulator controller (if provided).
        if (_emulator is null)
            throw new InvalidOperationException(
                "No emulator controller was provided, and GameContext.DeviceId is empty. " +
                "Provide DeviceId in GameContext OR provide IEmulatorController.");

        // Placeholder: use ProfileId as emulatorInstanceId (legacy compatibility).
        var emulatorInstanceId = context.ProfileId;

        await _emulator.EnsureInstanceRunningAsync(emulatorInstanceId, ct).ConfigureAwait(false);
        return await _emulator.ResolveDeviceIdAsync(emulatorInstanceId, ct).ConfigureAwait(false);
    }
}

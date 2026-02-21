/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Detection/WarAndOrderDetector.cs
 * Purpose: Library component: WarAndOrderDetector.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Common;
using GameContracts.Detection;
using GameModules.WarAndOrder.Common;
using GameModules.WarAndOrder.Ports;

namespace GameModules.WarAndOrder.Detection;

/// <summary>
/// Game detection logic for War and Order.
/// Later you can add:
/// - Template matching for UI buttons
/// - OCR to read resources
/// - Robust blocking-dialog detection
/// </summary>
public sealed class WarAndOrderDetector : IGameDetector
{
    private readonly IEmulatorController? _emulator;
    private readonly IDeviceAutomation _device;

    public WarAndOrderDetector(IDeviceAutomation device, IEmulatorController? emulator = null)
    {
        _device = device;
        _emulator = emulator;
    }

    public async Task<bool> IsInstalledAsync(GameContext context, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceIdAsync(context, ct);
        return await _device.IsPackageInstalledAsync(deviceId, WarAndOrderConstants.PackageName, ct);
    }

    public async Task<bool> IsMainScreenReadyAsync(GameContext context, CancellationToken ct)
    {
        // TODO: Real implementation should analyze screenshot and detect a stable "main UI" signature.
        // Placeholder: return true after short delay to keep flow unblocked during scaffolding.
        await Task.Delay(200, ct);
        return true;
    }

    public async Task<bool> IsTutorialCompletedAsync(GameContext context, CancellationToken ct)
    {
        // TODO: Detect tutorial completion from UI / local state.
        await Task.Delay(100, ct);
        return false;
    }

    public async Task<bool> HasBlockingDialogsAsync(GameContext context, CancellationToken ct)
    {
        // TODO: Detect blocking dialogs: network error, update required, permission dialogs, etc.
        await Task.Delay(100, ct);
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
                "Provide DeviceId in GameContext OR provide IEmulatorController implementation.");

        var emulatorInstanceId = context.ProfileId;
        await _emulator.EnsureInstanceRunningAsync(emulatorInstanceId, ct);
        return await _emulator.ResolveDeviceIdAsync(emulatorInstanceId, ct);
    }
}

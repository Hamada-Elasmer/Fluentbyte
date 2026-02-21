/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Lifecycle/WarAndOrderLifecycle.cs
 * Purpose: Library component: WarAndOrderLifecycle.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Common;
using GameContracts.Lifecycle;
using GameModules.WarAndOrder.Common;
using GameModules.WarAndOrder.Ports;

namespace GameModules.WarAndOrder.Lifecycle;

/// <summary>
/// Handles the lifecycle steps for War and Order:
/// - Install
/// - Launch
/// - Wait until ready
/// - Shutdown
/// 
/// The actual ADB/emulator details are delegated to ports (interfaces).
/// </summary>
public sealed class WarAndOrderLifecycle : IGameLifecycle
{
    private readonly IEmulatorController? _emulator;
    private readonly IDeviceAutomation _device;

    public WarAndOrderLifecycle(IDeviceAutomation device, IEmulatorController? emulator = null)
    {
        _device = device;
        _emulator = emulator;
    }

    public async Task InstallAsync(GameContext context, CancellationToken ct)
    {
        // NOTE:
        // We keep this method because the contract expects it.
        // Your Core might already handle install as a separate engine step.
        //
        // TODO: Implement installation flow:
        // 1) Resolve deviceId
        // 2) Check package installed
        // 3) If not installed -> install from Play Store / APK
        // 4) Log progress via your logger (outside this module or via an injected logger)
        var deviceId = await ResolveDeviceIdAsync(context, ct);

        var installed = await _device.IsPackageInstalledAsync(deviceId, WarAndOrderConstants.PackageName, ct);
        if (installed)
            return;

        // If you want this module to control installation, put your logic here.
        // For now we fail explicitly to avoid silent misbehavior.
        throw new NotImplementedException(
            "InstallAsync is not implemented yet. " +
            "Connect your install flow (Play Store / APK) using AdbLib/EmulatorLib.");
    }

    public async Task LaunchAsync(GameContext context, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceIdAsync(context, ct);

        // Make sure device is ready before launching.
        await _device.WaitForDeviceReadyAsync(deviceId, ct);

        // ✅ Focus/Launch the game via launcher intent (works even if MainActivity changes).
        await _device.LaunchPackageAsync(deviceId, WarAndOrderConstants.PackageName, ct);
    }

    public async Task WaitUntilReadyAsync(GameContext context, CancellationToken ct)
    {
        // TODO: Use detector (image detection / OCR) to confirm main screen is ready.
        // This module keeps it simple as a placeholder.
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }

    public async Task ShutdownAsync(GameContext context, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceIdAsync(context, ct);

        // Force stop to ensure clean state.
        await _device.ForceStopAsync(deviceId, WarAndOrderConstants.PackageName, ct);
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

        // Here we use ProfileId as a placeholder for emulatorInstanceId.
        // Replace this with your real mapping.
        var emulatorInstanceId = context.ProfileId;

        await _emulator.EnsureInstanceRunningAsync(emulatorInstanceId, ct);
        return await _emulator.ResolveDeviceIdAsync(emulatorInstanceId, ct);
    }
}

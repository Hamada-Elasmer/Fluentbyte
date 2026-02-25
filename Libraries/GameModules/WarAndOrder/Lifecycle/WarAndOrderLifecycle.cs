/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Lifecycle/WarAndOrderLifecycle.cs
 * Purpose: Library component: WarAndOrderLifecycle.
 * ============================================================================ */

using GameContracts.Common;
using GameContracts.Lifecycle;
using GameModules.WarAndOrder.Common;
using GameModules.WarAndOrder.Ports;

namespace GameModules.WarAndOrder.Lifecycle;

public sealed class WarAndOrderLifecycle(
    IDeviceAutomation device,
    IEmulatorController? emulator = null)
    : IGameLifecycle
{
    private const string LauncherPackage = "com.ldmnq.launcher3";

    public async Task InstallAsync(GameContext context, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceIdAsync(context, ct);

        var installed = await device.IsPackageInstalledAsync(
            deviceId,
            WarAndOrderConstants.PackageName,
            ct);

        if (installed)
            return;

        throw new NotImplementedException("InstallAsync is not implemented yet.");
    }

    public async Task LaunchAsync(GameContext context, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceIdAsync(context, ct);

        await device.WaitForDeviceReadyAsync(deviceId, ct);

        // If process already exists (fg/bg irrelevant) → skip launch
        var running = await device.IsProcessRunningAsync(
            deviceId,
            WarAndOrderConstants.PackageName,
            ct);

        if (running)
            return;

        // Kill LD launcher to prevent overlays
        await device.ForceStopAsync(deviceId, LauncherPackage, ct);

        // Launch via launcher intent
        await device.LaunchPackageAsync(
            deviceId,
            WarAndOrderConstants.PackageName,
            ct);

        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        // Popup killer (400x652 resolution)
        await device.TapAsync(deviceId, 380, 40, ct);
        await device.TapAsync(deviceId, 380, 40, ct);
    }

    public async Task WaitUntilReadyAsync(GameContext context, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }

    public async Task ShutdownAsync(GameContext context, CancellationToken ct)
    {
        var deviceId = await ResolveDeviceIdAsync(context, ct);

        await device.ForceStopAsync(
            deviceId,
            WarAndOrderConstants.PackageName,
            ct);
    }

    private async Task<string> ResolveDeviceIdAsync(GameContext context, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(context.DeviceId))
            return context.DeviceId!;

        if (emulator is null)
            throw new InvalidOperationException(
                "No emulator controller provided and GameContext.DeviceId is empty.");

        var emulatorInstanceId = context.ProfileId;

        await emulator.EnsureInstanceRunningAsync(emulatorInstanceId, ct);
        return await emulator.ResolveDeviceIdAsync(emulatorInstanceId, ct);
    }
}
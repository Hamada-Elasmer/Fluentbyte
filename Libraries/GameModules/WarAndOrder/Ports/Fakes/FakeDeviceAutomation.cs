/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Ports/Fakes/FakeDeviceAutomation.cs
 * Purpose: Fake implementation of IDeviceAutomation for testing.
 * ============================================================================ */

using GameModules.WarAndOrder.Ports;

namespace GameModules.WarAndOrder.Ports.Fakes;

public sealed class FakeDeviceAutomation : IDeviceAutomation
{
    public Task<bool> IsPackageInstalledAsync(string deviceId, string packageName, CancellationToken ct)
        => Task.FromResult(true);

    public Task LaunchActivityAsync(string deviceId, string activity, CancellationToken ct)
        => Task.CompletedTask;

    public Task LaunchPackageAsync(string deviceId, string packageName, CancellationToken ct)
        => Task.CompletedTask;

    public Task ForceStopAsync(string deviceId, string packageName, CancellationToken ct)
        => Task.CompletedTask;

    public Task<byte[]> ScreenshotAsync(string deviceId, CancellationToken ct)
        => Task.FromResult(Array.Empty<byte>());

    public Task TapAsync(string deviceId, int x, int y, CancellationToken ct)
        => Task.CompletedTask;

    public Task WaitForDeviceReadyAsync(string deviceId, CancellationToken ct)
        => Task.CompletedTask;

    public Task<bool> IsProcessRunningAsync(string deviceId, string packageName, CancellationToken ct)
        => Task.FromResult(false); // Fake always says "not running"
}
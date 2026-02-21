/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Ports/Fakes/FakeDeviceAutomation.cs
 * Purpose: Library component: FakeDeviceAutomation.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameModules.WarAndOrder.Ports.Fakes;

/// <summary>
/// A minimal fake implementation to allow unit testing of the module
/// without a real emulator/ADB.
/// 
/// DO NOT use in production.
/// </summary>
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
}

/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Ports/IDeviceAutomation.cs
 * Purpose: Library component: IDeviceAutomation port.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Drawing;

namespace GameModules.WarAndOrder.Ports;

public interface IDeviceAutomation
{
    Task<bool> IsPackageInstalledAsync(string deviceId, string packageName, CancellationToken ct);

    Task LaunchActivityAsync(string deviceId, string activity, CancellationToken ct);

    Task LaunchPackageAsync(string deviceId, string packageName, CancellationToken ct);

    Task ForceStopAsync(string deviceId, string packageName, CancellationToken ct);

    Task<byte[]> ScreenshotAsync(string deviceId, CancellationToken ct);

    Task TapAsync(string deviceId, int x, int y, CancellationToken ct);

    Task WaitForDeviceReadyAsync(string deviceId, CancellationToken ct);

    /// <summary>
    /// Checks whether a process (by package name) is currently running.
    /// Foreground/background does not matter.
    /// </summary>
    Task<bool> IsProcessRunningAsync(string deviceId, string packageName, CancellationToken ct);
}
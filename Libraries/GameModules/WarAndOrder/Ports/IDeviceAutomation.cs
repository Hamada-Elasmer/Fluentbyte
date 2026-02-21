/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Ports/IDeviceAutomation.cs
 * Purpose: Library component: in.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Drawing;

namespace GameModules.WarAndOrder.Ports;

/// <summary>
/// A small "port" (adapter interface) used by the game module.
/// Implement this interface in your Core/Libraries layer using AdbLib/EmulatorLib.
/// 
/// Why?
/// - WarAndOrder module stays clean & testable.
/// - You can swap ADB/emulator implementations without rewriting game logic.
/// </summary>
public interface IDeviceAutomation
{
    /// <summary>
    /// Checks if a package is installed on the given device.
    /// </summary>
    Task<bool> IsPackageInstalledAsync(string deviceId, string packageName, CancellationToken ct);

    /// <summary>
    /// Launches an app activity (package/activity).
    /// </summary>
    Task LaunchActivityAsync(string deviceId, string activity, CancellationToken ct);

    /// <summary>
    /// Launches (or focuses) an app using launcher intent (like tapping the icon).
    /// This is more stable than hardcoding MainActivity.
    /// </summary>
    Task LaunchPackageAsync(string deviceId, string packageName, CancellationToken ct);

    /// <summary>
    /// Force-stops an app package.
    /// </summary>
    Task ForceStopAsync(string deviceId, string packageName, CancellationToken ct);

    /// <summary>
    /// Takes a screenshot from the device. (Used for UI detection / OCR later.)
    /// </summary>
    Task<byte[]> ScreenshotAsync(string deviceId, CancellationToken ct);

    /// <summary>
    /// Performs a tap at (x,y) on the device screen.
    /// </summary>
    Task TapAsync(string deviceId, int x, int y, CancellationToken ct);

    /// <summary>
    /// Optional: waits until device is ready (adb-online, boot completed, etc.).
    /// </summary>
    Task WaitForDeviceReadyAsync(string deviceId, CancellationToken ct);
}

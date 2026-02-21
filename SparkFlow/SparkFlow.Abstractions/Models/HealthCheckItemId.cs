/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Models/HealthCheckItemId.cs
 * Purpose: Core component: HealthCheckItemId.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// Identifiers for all Health Check items.
/// IMPORTANT:
/// - Do NOT rename existing values.
/// - Do NOT reorder existing values.
/// - New items must be appended to avoid breaking mappings.
/// </summary>
public enum HealthCheckItemId
{
    // ----------------------------
    // General / Runtime checks
    // ----------------------------

    /// <summary>
    /// Verifies that required runtime folders exist.
    /// </summary>
    RuntimeFolders,

    /// <summary>
    /// Verifies that the Android device is ready and responsive via ADB.
    /// </summary>
    DeviceReady,

    /// <summary>
    /// Verifies that ADB is running and accessible.
    /// </summary>
    AdbRunning,

    /// <summary>
    /// Legacy (removed): emulator-specific ADB endpoint resolving (LdPlayer-style port mapping).
    /// </summary>
    LdPlayer_AdbEndpoint,

    // ----------------------------
    // Legacy emulator checks (removed)
    // ----------------------------

    /// <summary>
    /// Legacy (removed): LdPlayer installation check.
    /// </summary>
    LdPlayer_Installed,

    /// <summary>
    /// Legacy (removed): LdPlayer version check.
    /// </summary>
    LdPlayer_Version,

    // ----------------------------
    // Game-specific checks
    // ----------------------------

    /// <summary>
    /// Verifies that War and Order is installed in the selected emulator instance.
    /// </summary>
    Game_WarAndOrderInstalled,

    /// <summary>
    /// Ensures War and Order can launch and reach top activity.
    /// </summary>
    Game_WarAndOrderLaunch,

    // ----------------------------
    // NEW (SparkFlow v1 ADB-first checks)
    // IMPORTANT: Append only (do not reorder)
    // ----------------------------

    /// <summary>
    /// Opens the emulator instance and binds the best ADB serial to the profile.
    /// </summary>
    BindAdbSerialFromInstance,

    /// <summary>
    /// Ensures that the profile has a non-empty AdbSerial.
    /// </summary>
    Adb_SerialPresent,

    /// <summary>
    /// Ensures that the device appears in "adb devices".
    /// </summary>
    Adb_DeviceListed,

    /// <summary>
    /// Ensures that the device state is "device" (not offline/unauthorized).
    /// </summary>
    Adb_DeviceState,

    /// <summary>
    /// Ensures that ADB shell is responsive (basic ping).
    /// </summary>
    Adb_DeviceResponsive,

    /// <summary>
    /// Ensures screenshot works (exec-out screencap -p).
    /// </summary>
    Adb_ScreenshotWorks,

    /// <summary>
    /// Ensures device resolution is acceptable.
    /// </summary>
    Adb_ResolutionOk,
}

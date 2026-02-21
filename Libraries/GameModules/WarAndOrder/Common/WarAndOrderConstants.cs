/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Common/WarAndOrderConstants.cs
 * Purpose: Library component: WarAndOrderConstants.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameModules.WarAndOrder.Common;

/// <summary>
/// All constants for War and Order live here.
/// Keep these values in ONE place to avoid magic strings across the module.
/// </summary>
public static class WarAndOrderConstants
{
    /// <summary>
    /// Must match the value stored in your profile/game selection (Profile.GameId).
    /// </summary>
    public const string GameId = "wao";

    /// <summary>
    /// Android package name (verify on your devices/emulator).
    /// </summary>
    public const string PackageName = "com.camelgames.superking";

    /// <summary>
    /// Main launcher activity. If you are not sure, discover via:
    /// adb shell cmd package resolve-activity --brief com.camelgames.superking
    /// </summary>
    public const string MainActivity = "com.camelgames.superking/.MainActivity";

    /// <summary>
    /// Google Play Store URL for install/update flows (optional).
    /// </summary>
    public const string PlayStoreUrl = "https://play.google.com/store/apps/details?id=com.camelgames.wo";
}
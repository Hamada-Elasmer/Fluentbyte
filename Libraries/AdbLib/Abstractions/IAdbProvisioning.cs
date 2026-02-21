/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Abstractions/IAdbProvisioning.cs
 * Purpose: Library component: IAdbProvisioning.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace AdbLib.Abstractions;

public interface IAdbProvisioning
{
    /// <summary>
    /// Ensures ADB platform-tools exist in RuntimePlatformToolsDir and returns full path to adb.exe
    /// </summary>
    string EnsureProvisioned();
}
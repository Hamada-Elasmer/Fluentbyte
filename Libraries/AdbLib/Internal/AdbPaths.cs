/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Internal/AdbPaths.cs
 * Purpose: Library component: AdbPaths.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.IO;

namespace AdbLib.Internal;

internal sealed class AdbPaths
{
    public required string BundledDir { get; init; }
    public required string RuntimeDir { get; init; }
    public required string AdbExeName { get; init; }
    public required string VersionFileName { get; init; }

    public string RuntimeAdbExe => Path.Combine(RuntimeDir, AdbExeName);
    public string RuntimeVersionFile => Path.Combine(RuntimeDir, VersionFileName);
}

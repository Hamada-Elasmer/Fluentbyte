/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Options/AdbOptions.cs
 * Purpose: Library component: AdbOptions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;

namespace AdbLib.Options;

public sealed class AdbOptions
{
    /// <summary>Bundled platform-tools folder inside app output (e.g. AppBase/Assets/platform-tools).</summary>
    public required string BundledPlatformToolsDir { get; init; }

    /// <summary>Runtime platform-tools folder (e.g. AppBase/runtime/platform-tools).</summary>
    public required string RuntimePlatformToolsDir { get; init; }

    /// <summary>Bundled version stamp, e.g. "36.0.2".</summary>
    public required string BundledVersion { get; init; }

    /// <summary>File used to remember installed version inside runtime folder.</summary>
    public string VersionFileName { get; init; } = ".sparkflow_adb_version";

    /// <summary>Default timeout for adb commands.</summary>
    public int DefaultTimeoutMs { get; init; } = 30_000;

    /// <summary>How long to wait for device appearance / readiness.</summary>
    public int DeviceWaitTimeoutMs { get; init; } = 90_000;

    /// <summary>Poll interval when waiting on devices.</summary>
    public int DevicePollIntervalMs { get; init; } = 600;

    /// <summary>If true: always kill/start server at provisioning time.</summary>
    public bool RestartServerOnProvision { get; init; } = true;

    /// <summary>Override adb executable name if needed.</summary>
    public string AdbExeName { get; init; } =
        OperatingSystem.IsWindows() ? "adb.exe" : "adb";

    // ============================================================
    // ✅ Runtime-only policy (your requirement)
    // ============================================================

    /// <summary>
    /// If true: do NOT copy/replace runtime platform-tools. Assume RuntimePlatformToolsDir is managed externally.
    /// EnsureProvisioned will only validate runtime adb exists and return it.
    /// </summary>
    public bool RuntimeOnly { get; init; } = false;

    // ============================================================
    // ✅ Optional: TCP connect probing policy (helps LDPlayer new versions)
    // ============================================================

    /// <summary>
    /// If true: bind flows may attempt "adb connect 127.0.0.1:PORT" probing when DevicesAsync returns 0.
    /// </summary>
    public bool EnableTcpConnectProbe { get; init; } = true;

    /// <summary>Start port for TCP probe (odd ports are typical: 5555, 5557...).</summary>
    public int TcpConnectProbePortStart { get; init; } = 5555;

    /// <summary>End port for TCP probe (inclusive).</summary>
    public int TcpConnectProbePortEnd { get; init; } = 5585;

    /// <summary>Step for TCP probe ports (2 for 5555,5557,...).</summary>
    public int TcpConnectProbePortStep { get; init; } = 2;

    /// <summary>Maximum number of connect attempts per probe cycle (safety).</summary>
    public int TcpConnectProbeMaxAttemptsPerCycle { get; init; } = 6;
}
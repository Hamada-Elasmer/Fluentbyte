/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Abstractions/IAdbClient.cs
 * Purpose: Library component: IAdbClient.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdbLib.Models;

namespace AdbLib.Abstractions;

public interface IAdbClient
{
    string AdbExePath { get; }

    // ===============================
    // Async-first API
    // ===============================
    Task KillServerAsync(int timeoutMs = 12_000, CancellationToken ct = default);
    Task StartServerAsync(int timeoutMs = 20_000, CancellationToken ct = default);

    Task<IReadOnlyList<AdbDevice>> DevicesAsync(int timeoutMs = 15_000, CancellationToken ct = default);

    Task<string> ShellAsync(string serial, string shellCommand, int timeoutMs = 30_000, CancellationToken ct = default);
    Task<string> RunRawAsync(string arguments, int timeoutMs = 30_000, CancellationToken ct = default);

    // Existing: hard stop only (CancellationToken)
    Task WaitForDeviceReadyAsync(string serial, CancellationToken ct);

    // ✅ NEW: cooperative pause inside polling loops (PausePoint) + hard stop (CancellationToken)
    Task WaitForDeviceReadyAsync(
        string serial,
        Func<CancellationToken, Task> pausePointAsync,
        CancellationToken ct);

    // Package helpers
    Task StartPackageMonkeyAsync(string serial, string packageName, int timeoutMs = 30_000, CancellationToken ct = default);
    Task ForceStopPackageAsync(string serial, string packageName, int timeoutMs = 30_000, CancellationToken ct = default);
    Task<bool> IsPackageRunningAsync(string serial, string packageName, int timeoutMs = 15_000, CancellationToken ct = default);

    // Real launch
    Task StartActivityAsync(string serial, string component, int timeoutMs = 30_000, CancellationToken ct = default);
    Task<string> GetTopActivityAsync(string serial, int timeoutMs = 30_000, CancellationToken ct = default);

    // Screenshot
    Task<byte[]> ScreenshotPngAsync(string serial, int timeoutMs = 30_000, CancellationToken ct = default);

    // ===============================
    // Legacy synchronous API (kept for backwards compatibility)
    // ===============================
    void KillServer(int timeoutMs = 12_000);
    void StartServer(int timeoutMs = 20_000);

    IReadOnlyList<AdbDevice> Devices(int timeoutMs = 15_000);

    string Shell(string serial, string shellCommand, int timeoutMs = 30_000);
    string RunRaw(string arguments, int timeoutMs = 30_000);

    // Package helpers
    void StartPackageMonkey(string serial, string packageName, int timeoutMs = 30_000);
    void ForceStopPackage(string serial, string packageName, int timeoutMs = 30_000);
    bool IsPackageRunning(string serial, string packageName, int timeoutMs = 15_000);

    // Real launch
    void StartActivity(string serial, string component, int timeoutMs = 30_000);
    string GetTopActivity(string serial, int timeoutMs = 30_000);

    // Screenshot
    byte[] ScreenshotPng(string serial, int timeoutMs = 30_000);
}
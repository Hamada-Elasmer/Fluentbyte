/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Services/AdbClient.cs
 * Purpose: Library component: AdbClient.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdbLib.Abstractions;
using AdbLib.Exceptions;
using AdbLib.Internal;
using AdbLib.Models;
using AdbLib.Options;

namespace AdbLib.Services;

public sealed class AdbClient : IAdbClient
{
    private readonly AdbOptions _opt;
    public string AdbExePath { get; }

    public AdbClient(IAdbProvisioning provisioning, AdbOptions options)
    {
        _opt = options ?? throw new ArgumentNullException(nameof(options));
        AdbExePath = provisioning.EnsureProvisioned();
    }

    // ============================================================
    // Async-first API
    // ============================================================

    public Task KillServerAsync(int timeoutMs = 12_000, CancellationToken ct = default)
        => RunRawAsync("kill-server", timeoutMs, ct);

    public Task StartServerAsync(int timeoutMs = 20_000, CancellationToken ct = default)
        => RunRawAsync("start-server", timeoutMs, ct);

    public async Task<IReadOnlyList<AdbDevice>> DevicesAsync(int timeoutMs = 15_000, CancellationToken ct = default)
        => ParseDevices(await RunRawAsync("devices -l", timeoutMs, ct).ConfigureAwait(false));

    public Task<string> ShellAsync(string serial, string shellCommand, int timeoutMs = 30_000, CancellationToken ct = default)
        => RunArgsAsync(new[] { "-s", serial, "shell", shellCommand }, timeoutMs, ct);

    public async Task<string> RunRawAsync(string arguments, int timeoutMs = 30_000, CancellationToken ct = default)
    {
        var wd = Path.GetDirectoryName(AdbExePath)!;
        var args = Regex.Matches(arguments, "\"([^\"]*)\"|\\S+")
                        .Select(m => m.Value.Trim('"'))
                        .ToList();

        var r = await ProcessExec.RunAsync(AdbExePath, args, wd, timeoutMs, ct).ConfigureAwait(false);
        if (r.ExitCode != 0)
            throw new AdbCommandException("adb failed", r.ExitCode, r.StdOut, r.StdErr);

        return r.StdOut?.Trim() ?? "";
    }

    private async Task<string> RunArgsAsync(IReadOnlyList<string> args, int timeoutMs, CancellationToken ct)
    {
        var wd = Path.GetDirectoryName(AdbExePath)!;
        var r = await ProcessExec.RunAsync(AdbExePath, args, wd, timeoutMs, ct).ConfigureAwait(false);

        if (r.ExitCode != 0)
            throw new AdbCommandException("adb failed", r.ExitCode, r.StdOut, r.StdErr);

        return r.StdOut?.Trim() ?? "";
    }

    // ============================================================
    // Device ready wait (Polling)
    // ============================================================

    // Existing API (kept for compatibility)
    public async Task WaitForDeviceReadyAsync(string serial, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(_opt.DeviceWaitTimeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var d = (await DevicesAsync(ct: ct).ConfigureAwait(false)).FirstOrDefault(x => x.Serial == serial);
            if (d?.State == "device")
                return;

            await Task.Delay(_opt.DevicePollIntervalMs, ct).ConfigureAwait(false);
        }

        throw new AdbDeviceNotFoundException($"Device not ready: {serial}");
    }

    // NEW overload: supports PausePoint (cooperative pause inside polling)
    public async Task WaitForDeviceReadyAsync(
        string serial,
        Func<CancellationToken, Task> pausePointAsync,
        CancellationToken ct)
    {
        if (pausePointAsync is null) throw new ArgumentNullException(nameof(pausePointAsync));

        var deadline = DateTime.UtcNow.AddMilliseconds(_opt.DeviceWaitTimeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await pausePointAsync(ct).ConfigureAwait(false);

            var d = (await DevicesAsync(ct: ct).ConfigureAwait(false))
                .FirstOrDefault(x => x.Serial == serial);

            if (d?.State == "device")
                return;

            await pausePointAsync(ct).ConfigureAwait(false);
            await Task.Delay(_opt.DevicePollIntervalMs, ct).ConfigureAwait(false);
        }

        throw new AdbDeviceNotFoundException($"Device not ready: {serial}");
    }

    public Task StartPackageMonkeyAsync(string serial, string packageName, int timeoutMs = 30_000, CancellationToken ct = default)
        => ShellAsync(serial, $"monkey -p {packageName} 1", timeoutMs, ct);

    public Task ForceStopPackageAsync(string serial, string packageName, int timeoutMs = 30_000, CancellationToken ct = default)
        => ShellAsync(serial, $"am force-stop {packageName}", timeoutMs, ct);

    public async Task<bool> IsPackageRunningAsync(string serial, string packageName, int timeoutMs = 15_000, CancellationToken ct = default)
    {
        try
        {
            var r = await ShellAsync(serial, $"pidof {packageName}", timeoutMs, ct).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(r);
        }
        catch
        {
            return false;
        }
    }

    public Task StartActivityAsync(string serial, string component, int timeoutMs = 30_000, CancellationToken ct = default)
        => ShellAsync(serial,
            $"am start -n {component} -a android.intent.action.MAIN -c android.intent.category.LAUNCHER",
            timeoutMs,
            ct);

    public async Task<string> GetTopActivityAsync(string serial, int timeoutMs = 30_000, CancellationToken ct = default)
    {
        var txt = await ShellAsync(serial, "dumpsys window windows", timeoutMs, ct).ConfigureAwait(false);
        var idx = txt.IndexOf("mCurrentFocus", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? txt.Substring(idx, Math.Min(200, txt.Length - idx)) : txt;
    }

    // ============================================================
    // Screenshot (Async)
    // ============================================================
    public async Task<byte[]> ScreenshotPngAsync(string serial, int timeoutMs = 30_000, CancellationToken ct = default)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"sf_shot_{Guid.NewGuid():N}.png");
        var remote = "/sdcard/__sf_shot.png";

        try
        {
            await ShellAsync(serial, $"rm -f {remote}", timeoutMs, ct).ConfigureAwait(false);
            await ShellAsync(serial, $"screencap -p {remote}", timeoutMs, ct).ConfigureAwait(false);
            await RunArgsAsync(new[] { "-s", serial, "pull", remote, tmp }, timeoutMs, ct).ConfigureAwait(false);
            return await File.ReadAllBytesAsync(tmp, ct).ConfigureAwait(false);
        }
        finally
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        }
    }

    // ============================================================
    // Legacy sync wrappers (backwards compatibility)
    // ============================================================

    public void KillServer(int timeoutMs = 12_000)
        => KillServerAsync(timeoutMs).GetAwaiter().GetResult();

    public void StartServer(int timeoutMs = 20_000)
        => StartServerAsync(timeoutMs).GetAwaiter().GetResult();

    public IReadOnlyList<AdbDevice> Devices(int timeoutMs = 15_000)
        => DevicesAsync(timeoutMs).GetAwaiter().GetResult();

    public string Shell(string serial, string shellCommand, int timeoutMs = 30_000)
        => ShellAsync(serial, shellCommand, timeoutMs).GetAwaiter().GetResult();

    public string RunRaw(string arguments, int timeoutMs = 30_000)
        => RunRawAsync(arguments, timeoutMs).GetAwaiter().GetResult();

    public void StartPackageMonkey(string serial, string packageName, int timeoutMs = 30_000)
        => StartPackageMonkeyAsync(serial, packageName, timeoutMs).GetAwaiter().GetResult();

    public void ForceStopPackage(string serial, string packageName, int timeoutMs = 30_000)
        => ForceStopPackageAsync(serial, packageName, timeoutMs).GetAwaiter().GetResult();

    public bool IsPackageRunning(string serial, string packageName, int timeoutMs = 15_000)
        => IsPackageRunningAsync(serial, packageName, timeoutMs).GetAwaiter().GetResult();

    public void StartActivity(string serial, string component, int timeoutMs = 30_000)
        => StartActivityAsync(serial, component, timeoutMs).GetAwaiter().GetResult();

    public string GetTopActivity(string serial, int timeoutMs = 30_000)
        => GetTopActivityAsync(serial, timeoutMs).GetAwaiter().GetResult();

    // ============================================================
    // Screenshot
    // ============================================================
    public byte[] ScreenshotPng(string serial, int timeoutMs = 30_000)
        => ScreenshotPngAsync(serial, timeoutMs).GetAwaiter().GetResult();

    private static IReadOnlyList<AdbDevice> ParseDevices(string text)
    {
        var list = new List<AdbDevice>();
        if (string.IsNullOrWhiteSpace(text)) return list;

        foreach (var raw in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.StartsWith("List of devices") || line.StartsWith("*"))
                continue;

            var parts = Regex.Split(line, @"\s+");
            if (parts.Length >= 2)
                list.Add(new AdbDevice(parts[0], parts[1]));
        }

        return list;
    }
}
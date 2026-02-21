/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.Core/Services/Game/Ports/DeviceAutomationAdapter.cs
 * Purpose: Core adapter: implements WarAndOrder port IDeviceAutomation using IDeviceSessionFactory (ADB-first).
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameModules.WarAndOrder.Ports;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Game.Ports;

/// <summary>
/// ADB-first adapter for WarAndOrder's IDeviceAutomation port.
/// Uses IDeviceSessionFactory to ensure exclusive access per deviceId (adb serial).
/// </summary>
public sealed class DeviceAutomationAdapter : IDeviceAutomation
{
    private readonly IDeviceSessionFactory _sessions;

    public DeviceAutomationAdapter(IDeviceSessionFactory sessions)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    public async Task<bool> IsPackageInstalledAsync(string deviceId, string packageName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException(nameof(packageName));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        // "pm list packages <pkg>" returns lines containing the package if installed.
        var output = await dev.ShellAsync($"pm list packages {packageName}", timeoutMs: 12_000, cancellationToken: ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(output))
            return false;

        return output.IndexOf(packageName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public async Task LaunchActivityAsync(string deviceId, string activity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        if (string.IsNullOrWhiteSpace(activity))
            throw new ArgumentException(nameof(activity));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        // activity should be "package/activity"
        await dev.StartActivityAsync(activity.Trim(), timeoutMs: 20_000, cancellationToken: ct)
            .ConfigureAwait(false);
    }

    public async Task LaunchPackageAsync(string deviceId, string packageName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException(nameof(packageName));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        // ✅ Launch/focus via launcher intent (equivalent to tapping the app icon).
        // Works even if MainActivity changes across versions.
        await dev.ShellAsync(
                $"monkey -p {packageName.Trim()} -c android.intent.category.LAUNCHER 1",
                timeoutMs: 20_000,
                cancellationToken: ct)
            .ConfigureAwait(false);
    }

    public async Task ForceStopAsync(string deviceId, string packageName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException(nameof(packageName));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        await dev.ForceStopAsync(packageName.Trim(), timeoutMs: 10_000, cancellationToken: ct)
            .ConfigureAwait(false);
    }

    public async Task<byte[]> ScreenshotAsync(string deviceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        // We need binary PNG, but ShellAsync returns string. Use base64 pipe.
        // Note: Some Android builds wrap base64 output with newlines; we remove whitespace before decoding.
        var b64 = await dev.ShellAsync("screencap -p | base64", timeoutMs: 30_000, cancellationToken: ct)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(b64))
            return Array.Empty<byte[]>() is byte[][]? Array.Empty<byte>() : Array.Empty<byte>();

        // Remove whitespace/newlines
        var cleaned = new string(b64.Where(ch => !char.IsWhiteSpace(ch)).ToArray());

        try
        {
            return Convert.FromBase64String(cleaned);
        }
        catch
        {
            // Fallback: return empty if the device doesn't support base64 / output is corrupted.
            return Array.Empty<byte>();
        }
    }

    public async Task TapAsync(string deviceId, int x, int y, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        // Android input tap
        await dev.ShellAsync($"input tap {x} {y}", timeoutMs: 8_000, cancellationToken: ct)
            .ConfigureAwait(false);
    }

    public async Task WaitForDeviceReadyAsync(string deviceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException(nameof(deviceId));

        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct).ConfigureAwait(false);
        await dev.WaitUntilReadyAsync(ct).ConfigureAwait(false);
    }
}

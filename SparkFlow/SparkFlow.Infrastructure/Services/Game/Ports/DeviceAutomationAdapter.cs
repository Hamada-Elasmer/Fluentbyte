/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Infrastructure/Services/Game/Ports/DeviceAutomationAdapter.cs
 * Purpose: Core adapter implementing WarAndOrder IDeviceAutomation via IDeviceSessionFactory.
 * ============================================================================ */

using GameModules.WarAndOrder.Ports;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Game.Ports;

public sealed class DeviceAutomationAdapter : IDeviceAutomation
{
    private readonly IDeviceSessionFactory _sessions;

    public DeviceAutomationAdapter(IDeviceSessionFactory sessions)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
    }

    public async Task<bool> IsPackageInstalledAsync(string deviceId, string packageName, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        var output = await dev.ShellAsync(
            $"pm list packages {packageName}",
            timeoutMs: 12_000,
            cancellationToken: ct);

        return !string.IsNullOrWhiteSpace(output) &&
               output.IndexOf(packageName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public async Task LaunchActivityAsync(string deviceId, string activity, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        await dev.StartActivityAsync(activity.Trim(), timeoutMs: 20_000, cancellationToken: ct);
    }

    public async Task LaunchPackageAsync(string deviceId, string packageName, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        await dev.ShellAsync(
            $"monkey -p {packageName.Trim()} -c android.intent.category.LAUNCHER 1",
            timeoutMs: 20_000,
            cancellationToken: ct);
    }

    public async Task ForceStopAsync(string deviceId, string packageName, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        await dev.ForceStopAsync(packageName.Trim(), timeoutMs: 10_000, cancellationToken: ct);
    }

    public async Task<byte[]> ScreenshotAsync(string deviceId, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        var b64 = await dev.ShellAsync(
            "screencap -p | base64",
            timeoutMs: 30_000,
            cancellationToken: ct);

        if (string.IsNullOrWhiteSpace(b64))
            return Array.Empty<byte>();

        var cleaned = new string(b64.Where(ch => !char.IsWhiteSpace(ch)).ToArray());

        try
        {
            return Convert.FromBase64String(cleaned);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public async Task TapAsync(string deviceId, int x, int y, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        await dev.ShellAsync(
            $"input tap {x} {y}",
            timeoutMs: 8_000,
            cancellationToken: ct);
    }

    public async Task WaitForDeviceReadyAsync(string deviceId, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);
    }

    public async Task<bool> IsProcessRunningAsync(string deviceId, string packageName, CancellationToken ct)
    {
        await using var dev = await _sessions.OpenSessionAsync(deviceId.Trim(), ct);
        await dev.WaitUntilReadyAsync(ct);

        // Fast path: pidof
        var output = await dev.ShellAsync(
            $"pidof {packageName.Trim()}",
            timeoutMs: 8_000,
            cancellationToken: ct);

        if (!string.IsNullOrWhiteSpace(output))
            return true;

        // Fallback for older Android builds
        var ps = await dev.ShellAsync(
            "ps",
            timeoutMs: 12_000,
            cancellationToken: ct);

        return !string.IsNullOrWhiteSpace(ps) &&
               ps.IndexOf(packageName.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
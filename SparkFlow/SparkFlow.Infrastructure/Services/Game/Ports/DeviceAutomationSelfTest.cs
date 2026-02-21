using AdbLib.Abstractions;
using GameModules.WarAndOrder.Ports;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Game.Ports;

public sealed class DeviceAutomationSelfTest
{
    private readonly IAdbClient _adb;
    private readonly IDeviceAutomation _auto;
    private readonly MLogger _log;

    public DeviceAutomationSelfTest(IAdbClient adb, IDeviceAutomation auto, MLogger logger)
    {
        _adb = adb;
        _auto = auto;
        _log = logger ?? MLogger.Instance;
    }

    public async Task RunAsync(string? deviceId, CancellationToken ct)
    {
        _log.Info(LogChannel.SYSTEM, "[SelfTest] Starting DeviceAutomation self-test...");

        // 1) pick a device if not provided
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            // relies on your existing AdbClient behavior; if you don't have a method, we fallback to shell.
            // Most AdbClient implementations expose "ListDevices" but if yours doesn't, we do a shell parse.
            var raw = _adb.Shell("", "devices", 8000); // if your AdbClient doesn't support this, tell me.
            // If this line fails in your codebase, we'll replace it with the real list method you have.
            throw new NotSupportedException("Please pass deviceId explicitly for now.");
        }

        deviceId = deviceId.Trim();

        // 2) wait ready
        await _auto.WaitForDeviceReadyAsync(deviceId, ct);
        _log.Info(LogChannel.SYSTEM, $"[SelfTest] Device ready: {deviceId}");

        // 3) screenshot
        var png = await _auto.ScreenshotAsync(deviceId, ct);
        if (png.Length == 0)
        {
            _log.Warn(LogChannel.SYSTEM, "[SelfTest] Screenshot returned empty bytes.");
        }
        else
        {
            var outDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "SparkFlow_SelfTest");

            Directory.CreateDirectory(outDir);

            var path = Path.Combine(outDir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            await File.WriteAllBytesAsync(path, png, ct);

            _log.Info(LogChannel.SYSTEM, $"[SelfTest] Screenshot saved: {path}");
        }

        // 4) tap center-ish (safe)
        await _auto.TapAsync(deviceId, 540, 960, ct);
        _log.Info(LogChannel.SYSTEM, "[SelfTest] Tap executed at (540, 960)");

        // 5) launch Wo (if installed)
        var pkg = "com.camelgames.wo";
        var installed = await _auto.IsPackageInstalledAsync(deviceId, pkg, ct);
        _log.Info(LogChannel.SYSTEM, $"[SelfTest] WarAndOrder installed: {installed}");

        if (installed)
        {
            await _auto.LaunchActivityAsync(deviceId, "com.camelgames.wo/com.camelgames.wo.MainActivity", ct);
            _log.Info(LogChannel.SYSTEM, "[SelfTest] WarAndOrder launch command sent ✅");
        }
        else
        {
            _log.Warn(LogChannel.SYSTEM, "[SelfTest] WarAndOrder not installed on this device.");
        }

        _log.Info(LogChannel.SYSTEM, "[SelfTest] Done ✅");
    }
}

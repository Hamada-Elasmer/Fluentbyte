/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Runner/AppShutdownService.cs
 * Purpose: Core component: AppShutdownService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */


using System.Diagnostics;
using AdbLib.Options;
using Microsoft.Extensions.DependencyInjection;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Services.Emulator.Guards;

namespace SparkFlow.Engine.Runner;

public sealed class AppShutdownService : IAppShutdownService
{
    private readonly IServiceProvider _sp;

    // Tunables (professional: split into small time budgets).
    private const int RunnerStopBudgetMs = 2500;
    private const int EmulatorStopBudgetMs = 6000;
    private const int EmulatorPerInstanceMs = 6000;
    private const int AdbBudgetMs = 1500;

    public AppShutdownService(IServiceProvider sp)
    {
        _sp = sp;
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        // 1) Stop the runner with a short time budget (do not wait forever).
        try
        {
            var runner = _sp.GetRequiredService<IGlobalRunnerService>();
            await RunWithTimeoutAsync(() => runner.StopAsync(), RunnerStopBudgetMs, ct);
        }
        catch { }

        // 2) Kill emulator instances + host with a clear time budget.
        try
        {
            var emu = _sp.GetRequiredService<IEmulatorInstanceControlService>();
            await RunWithTimeoutAsync(
                () => emu.EmergencyStopAllAsync(
                    overallTimeoutMs: EmulatorStopBudgetMs,
                    perInstanceTimeoutMs: EmulatorPerInstanceMs,
                    maxParallelStops: 3,
                    ct: CancellationToken.None),
                EmulatorStopBudgetMs + 500,
                ct);
        }
        catch { }

        // 3) Kill ADB server + adb.exe
        try
        {
            await RunWithTimeoutAsync(() => KillAdbEverythingAsync(_sp), AdbBudgetMs, ct);
        }
        catch { }
    }

    private static async Task RunWithTimeoutAsync(Func<Task> action, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0) timeoutMs = 1500;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        var task = action();

        try
        {
            await task.WaitAsync(timeoutCts.Token);
        }
        catch
        {
            // Ignore timeouts or errors; shutdown must continue.
        }
    }

    private static async Task KillAdbEverythingAsync(IServiceProvider sp)
    {
        var adbPath = ResolveAdbPathSafe(sp);

        // Prefer killing the ADB server first.
        if (!string.IsNullOrWhiteSpace(adbPath) && File.Exists(adbPath))
        {
            try
            {
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = adbPath,
                    Arguments = "kill-server",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(adbPath) ?? AppContext.BaseDirectory
                };

                p.Start();
                await Task.WhenAny(p.WaitForExitAsync(), Task.Delay(1200));
            }
            catch { }
        }

        // Kill any remaining adb.exe processes.
        try
        {
            foreach (var proc in Process.GetProcessesByName("adb"))
            {
                try { proc.Kill(entireProcessTree: true); }
                catch { }
            }
        }
        catch { }
    }

    private static string? ResolveAdbPathSafe(IServiceProvider sp)
    {
        try
        {
            var opt = sp.GetRequiredService<AdbOptions>();

            var runtimeAdb = Path.Combine(opt.RuntimePlatformToolsDir ?? "", "adb.exe");
            if (!string.IsNullOrWhiteSpace(opt.RuntimePlatformToolsDir) && File.Exists(runtimeAdb))
                return runtimeAdb;

            var bundledAdb = Path.Combine(opt.BundledPlatformToolsDir ?? "", "adb.exe");
            if (!string.IsNullOrWhiteSpace(opt.BundledPlatformToolsDir) && File.Exists(bundledAdb))
                return bundledAdb;

            var baseDir = AppContext.BaseDirectory;
            var assetsAdb = Path.Combine(baseDir, "Assets", "platform-tools", "adb.exe");
            if (File.Exists(assetsAdb))
                return assetsAdb;
        }
        catch { }

        return null;
    }
}
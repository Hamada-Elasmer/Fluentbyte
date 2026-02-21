/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.UI/Views/Shell/MainWindow.axaml.cs
 * Purpose: UI component: MainWindow.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using AdbLib.Options;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using SettingsStore.Interfaces;
using SparkFlow.UI.ViewModels.Shell;
using SukiUI.Controls;
using SukiUI.Enums;
using SukiUI.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.Views.Shell;

public partial class MainWindow : SukiWindow
{
    private readonly MainWindowViewModel _viewModel;

    private int _closingOnce;

    public MainWindow()
    {
        InitializeComponent();
        if (RuntimeFeature.IsDynamicCodeCompiled == false)
        {
            Title += " (native)";
        }

        var sp = App.ServiceProvider
            ?? throw new InvalidOperationException("App.ServiceProvider is null (DI not initialized).");

        var settingsAccessor = sp.GetRequiredService<ISettingsAccessor>();
        _viewModel = sp.GetRequiredService<MainWindowViewModel>();

        DataContext = _viewModel;

        Width = settingsAccessor.Current.SizeWidth;
        Height = settingsAccessor.Current.SizeHeight;
        Title = settingsAccessor.Current.AppTitle;

        // Professional: close immediately and clean up in the background.
        Closing += OnClosingFast;
    }

    // ==========================================================
    // Demo Menu Handlers (Theme / Background / Menu Toggle)
    // ==========================================================
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        IsMenuVisible = !IsMenuVisible;
    }

    private void AppIcon_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Demo behavior: click the logo to toggle the top menu.
        IsMenuVisible = !IsMenuVisible;
    }

    // ==========================================================
    // Fast Close (Professional)
    // ==========================================================
    private void OnClosingFast(object? sender, WindowClosingEventArgs e)
    {
        // Prevent double execution.
        if (Interlocked.Exchange(ref _closingOnce, 1) == 1)
            return;

        // Do not block the close: let the window close immediately.
        // Run fast cleanup in the background without blocking the UI.
        _ = Task.Run(async () =>
        {
            try
            {
                await BestEffortCleanupAsync();
            }
            catch
            {
                // ignore
            }
            finally
            {
                // Guarantee exit even if any service hangs.
                try { Environment.Exit(0); }
                catch
                {
                    // ignored
                }
            }
        });
    }

    private static async Task BestEffortCleanupAsync()
    {
        var sp = SparkFlow.UI.App.ServiceProvider;
        if (sp is null) return;

        // Short timeouts (professional feel).
        const int runnerMs = 800;
        const int emulatorMs = 1200;
        const int adbMs = 600;

        // 1) Stop Runner (StopAsync without CT -> use WhenAny).
        try
        {
            var runner = sp.GetRequiredService<IGlobalRunnerService>();
            await Task.WhenAny(runner.StopAsync(), Task.Delay(runnerMs));
        }
        catch (Exception ex)
        {
            MLogger.Instance.Exception(LogChannel.SYSTEM, ex, "[Shutdown] Runner stop failed");
        }

        // 2) Stop Emulator (if it supports cancellation).
        try
        {
            var emulator = sp.GetRequiredService<IEmulatorInstanceControlService>();
            using var cts = new CancellationTokenSource(emulatorMs);
            await Task.WhenAny(emulator.EmergencyStopAllAsync(cts.Token), Task.Delay(emulatorMs, cts.Token));
        }
        catch (Exception ex)
        {
            MLogger.Instance.Exception(LogChannel.SYSTEM, ex, "[Shutdown] Emulator stop failed");
        }

        // 3) Kill ADB ( the best effort).
        try
        {
            await Task.WhenAny(KillAdbAsync(sp), Task.Delay(adbMs));
        }
        catch (Exception ex)
        {
            MLogger.Instance.Exception(LogChannel.SYSTEM, ex, "[Shutdown] KillAdbAsync failed");
        }
    }

    // ==========================================================
    // ADB shutdown helpers (fast & safe)
    // ==========================================================
    private static async Task KillAdbAsync(IServiceProvider sp)
    {
        var adbPath = ResolveAdbPathSafe(sp);
        if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
            return;

        // 1) adb kill-server (short wait + kill)
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

            _ = p.Start();

            var waitTask = p.WaitForExitAsync();
            var done = await Task.WhenAny(waitTask, Task.Delay(400));
            if (done != waitTask)
            {
                try { p.Kill(entireProcessTree: true); } catch { }
            }
        }
        catch
        {
            // ignore
        }

        // 2) kill any remaining adb.exe
        try
        {
            foreach (var proc in Process.GetProcessesByName("adb"))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string? ResolveAdbPathSafe(IServiceProvider sp)
    {
        try
        {
            var opt = sp.GetRequiredService<AdbOptions>();

            if (!string.IsNullOrWhiteSpace(opt.RuntimePlatformToolsDir))
            {
                var runtime = Path.Combine(opt.RuntimePlatformToolsDir, "adb.exe");
                if (File.Exists(runtime)) return runtime;
            }

            if (!string.IsNullOrWhiteSpace(opt.BundledPlatformToolsDir))
            {
                var bundled = Path.Combine(opt.BundledPlatformToolsDir, "adb.exe");
                if (File.Exists(bundled)) return bundled;
            }

            var assetsAdb = Path.Combine(AppContext.BaseDirectory, "Assets", "platform-tools", "adb.exe");
            if (File.Exists(assetsAdb)) return assetsAdb;
        }
        catch
        {
            // ignore
        }

        return null;
    }
}

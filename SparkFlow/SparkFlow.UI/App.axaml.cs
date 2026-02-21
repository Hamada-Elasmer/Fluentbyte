/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.UI/App.axaml.cs
 * Purpose: Avalonia application bootstrap (DI + logging + plugins + main window).
 * Notes:
 *  - Comments are intentionally kept in English for consistency across the codebase.
 *  - Keep boot order stable: Logging -> Folders -> Settings -> Core -> UI -> Plugins -> MainWindow.
 * ============================================================================ 
 */

using AdbLib.Abstractions;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using GameContracts.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SettingsStore;
using SettingsStore.Interfaces;
using SettingsStore.Providers;
using SparkFlow.UI.Services.Dialogs;
using SparkFlow.UI.Services.Navigation;
using SparkFlow.UI.Services.Plugins;
using SparkFlow.UI.Services.Windows;
using SparkFlow.UI.ViewModels.Dialogs.Contents;
using SparkFlow.UI.ViewModels.Pages;
using SparkFlow.UI.ViewModels.Pages.Accounts;
using SparkFlow.UI.ViewModels.Pages.Home;
using SparkFlow.UI.ViewModels.Pages.Settings;
using SparkFlow.UI.ViewModels.Shell;
using SparkFlow.UI.ViewModels.Shell.Controls;
using SparkFlow.UI.ViewModels.Windows.Accounts;
using SparkFlow.UI.Views.Dialogs.Contents;
using SparkFlow.UI.Views.Pages;
using SparkFlow.UI.Views.Pages.Accounts;
using SparkFlow.UI.Views.Shell;
using SparkFlow.UI.Views.Windows.Accounts;
using SukiUI;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Dialogs;
using SparkFlow.Abstractions.Services.Health;
using SparkFlow.Abstractions.Services.Logging;
using SparkFlow.Infrastructure.Services.Health;
using SparkFlow.Infrastructure.Services.Logging;
using SparkFlow.Infrastructure.Services.Monitoring;
using UtiliLib;
using UtiliLib.Types;

// ✅ NEW: Core DI bootstrap extension (AddCoreServices)
using SparkFlow.Bootstrap.Bootstrap;

[assembly: SupportedOSPlatform("windows")]

namespace SparkFlow.UI;

public class App : Application
{
    // Settings provider is static to ensure a single source of truth across the app lifetime.
    private static ISettingsProvider SettingsProvider { get; }

    // Exposed for legacy access patterns (some windows/services may resolve from here).
    public static IServiceProvider? ServiceProvider { get; private set; }

    static App()
    {
        // Resolve the settings file path relative to the deployed app.
        var baseDir = AppContext.BaseDirectory;
        var settingsPath = Path.Combine(baseDir, "runtime", "settings", "app-settings.json");

        // File-backed settings provider (creates/reads JSON settings).
        SettingsProvider = new FileSettingsProvider(settingsPath);
    }

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // =========================================================
        // 1) Logging bootstrap (MUST be first)
        // =========================================================
        MLogger.Instance.Init();

        var baseDir = AppContext.BaseDirectory;

        // =========================================================
        // 2) Ensure runtime folders exist (never assume they exist)
        // =========================================================
        Directory.CreateDirectory(Path.Combine(baseDir, "runtime", "profiles"));
        Directory.CreateDirectory(Path.Combine(baseDir, "runtime", "settings"));
        Directory.CreateDirectory(Path.Combine(baseDir, "runtime", "logs"));
        Directory.CreateDirectory(Path.Combine(baseDir, "games"));

        // =========================================================
        // 3) DI container bootstrap
        // =========================================================
        var services = new ServiceCollection();

        // =========================================================
        // 4) Settings (Accessor is the UI/Core gateway)
        // =========================================================
        var settingsAccessor = new SettingsAccessor(SettingsProvider);
        services.AddSingleton<ISettingsAccessor>(settingsAccessor);

        // =========================================================
        // 5) Core (DI bootstrap) ✅ NEW
        //    Registers: ILogHub, ADB, Emulator, Health, Engine, Monitoring ...etc
        // =========================================================
        services.AddCoreServices(settingsAccessor);

        // =========================================================
        // 6) SukiUI infrastructure (required for ToastHost/DialogHost)
        // =========================================================
        services.AddSingleton<ISukiToastManager, SukiToastManager>();
        services.AddSingleton<ISukiDialogManager, SukiDialogManager>();

        // =========================================================
        // 7) Navigation Service (central navigation requests)
        // =========================================================
        services.AddSingleton<PageNavigationService>();

        // IMPORTANT: map interface to the SAME instance (avoid two singletons)
        services.AddSingleton<IPageNavigationService>(sp =>
            sp.GetRequiredService<PageNavigationService>());

        // =========================================================
        // 8) Dialog Services (ALL SUKI)
        // =========================================================

        // Core dialogs: Info/Warning/Confirm
        services.AddSingleton<SukiDialogService>();
        services.AddSingleton<IDialogService>(sp => sp.GetRequiredService<SukiDialogService>());

        // App dialogs: includes core + content dialogs
        services.AddSingleton<SukiAppDialogService>();
        services.AddSingleton<IAppDialogService>(sp => sp.GetRequiredService<SukiAppDialogService>());

        // =========================================================
        // 9) Main ViewModels
        // =========================================================
        services.AddSingleton<AppInfoViewModel>();
        services.AddSingleton<HomePageViewModel>();
        services.AddSingleton<AccountsPageViewModel>();
        services.AddSingleton<LogsPageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton<AppControlViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // =========================================================
        // 10) Pages (Views) - DataContext is wired here for safety
        // =========================================================
        services.AddTransient<HomePageView>(sp => new HomePageView
        {
            DataContext = sp.GetRequiredService<HomePageViewModel>()
        });

        services.AddTransient<AccountsPageView>(sp => new AccountsPageView
        {
            DataContext = sp.GetRequiredService<AccountsPageViewModel>()
        });

        services.AddTransient<LogsPageView>(sp => new LogsPageView
        {
            DataContext = sp.GetRequiredService<LogsPageViewModel>()
        });

        services.AddTransient<SettingsPageView>(sp => new SettingsPageView
        {
            DataContext = sp.GetRequiredService<SettingsPageViewModel>()
        });

        // Factories for navigation (MainWindowViewModel uses these).
        services.AddSingleton<Func<HomePageView>>(sp => () => sp.GetRequiredService<HomePageView>());
        services.AddSingleton<Func<AccountsPageView>>(sp => () => sp.GetRequiredService<AccountsPageView>());
        services.AddSingleton<Func<LogsPageView>>(sp => () => sp.GetRequiredService<LogsPageView>());
        services.AddSingleton<Func<SettingsPageView>>(sp => () => sp.GetRequiredService<SettingsPageView>());

        // =========================================================
        // 11) Account Windows (required by Accounts Page actions)
        // =========================================================
        services.AddTransient<AccountTasksWindowViewModel>();
        services.AddTransient<AccountDashboardWindowViewModel>();
        services.AddTransient<AccountHealthCheckWindowViewModel>();

        services.AddTransient<AccountTasksWindow>(sp => new AccountTasksWindow
        {
            DataContext = sp.GetRequiredService<AccountTasksWindowViewModel>()
        });

        services.AddTransient<AccountDashboardWindow>(sp => new AccountDashboardWindow
        {
            DataContext = sp.GetRequiredService<AccountDashboardWindowViewModel>()
        });

        services.AddSingleton<Func<AccountTasksWindow>>(sp => () => sp.GetRequiredService<AccountTasksWindow>());
        services.AddSingleton<Func<AccountDashboardWindow>>(sp => () => sp.GetRequiredService<AccountDashboardWindow>());

        services.AddSingleton<IAccountWindowsService, AccountWindowsService>();

        // =========================================================
        // 12) Health Dialog Content (VM + Content + Factory)
        // =========================================================
        services.AddTransient<AccountHealthCheckDialogContentViewModel>();

        services.AddTransient<AccountHealthCheckDialogContent>(sp =>
        {
            var vm = sp.GetRequiredService<AccountHealthCheckDialogContentViewModel>();

            vm.SetRunner(sp.GetRequiredService<HealthCheckRunner>());
            vm.SetHealthService(sp.GetRequiredService<IHealthCheckService>());

            return new AccountHealthCheckDialogContent
            {
                DataContext = vm
            };
        });

        services.AddSingleton<Func<AccountHealthCheckDialogContent>>(sp =>
            () => sp.GetRequiredService<AccountHealthCheckDialogContent>());

        // =========================================================
        // 12.5) HealthCheck Dialog Factory (Profile-bound) - ✅ Typed
        // =========================================================
        services.AddSingleton<HealthDialogContentFactory>(sp => (profileId) =>
        {
            try
            {
                var selector = sp.GetRequiredService<IAccountsSelector>();
                selector.Select(profileId);
            }
            catch
            {
                // ignored
            }

            var vm = sp.GetRequiredService<AccountHealthCheckDialogContentViewModel>();
            vm.SetRunner(sp.GetRequiredService<HealthCheckRunner>());
            vm.SetHealthService(sp.GetRequiredService<IHealthCheckService>());

            // ✅ IMPORTANT: bind profile to trigger Activate() + build rows + auto-run
            vm.BoundProfileId = profileId?.Trim() ?? "";

            return new AccountHealthCheckDialogContent
            {
                DataContext = vm
            };
        });

        // =========================================================
        // 13) Game Info Dialog Content (VM + Content + Factory)
        // =========================================================
        services.AddTransient<GameInfoDialogContentViewModel>();

        services.AddTransient<GameInfoDialogContent>(sp =>
        {
            var vm = sp.GetRequiredService<GameInfoDialogContentViewModel>();
            return new GameInfoDialogContent { DataContext = vm };
        });

        services.AddSingleton<Func<GameInfoDialogContent>>(sp =>
            () => sp.GetRequiredService<GameInfoDialogContent>());

        // =========================================================
        // 13.5) Game Dialog Factory (Profile-bound) - ✅ Typed
        // =========================================================
        services.AddSingleton<GameDialogContentFactory>(sp => (profileId) =>
        {
            try
            {
                var selector = sp.GetRequiredService<IAccountsSelector>();
                selector.Select(profileId);
            }
            catch
            {
                // ignored
            }

            var vm = sp.GetRequiredService<GameInfoDialogContentViewModel>();

            // ✅ must set services before activate
            vm.SetServices(
                sp.GetRequiredService<IAdbClient>(),
                sp.GetRequiredService<IProfilesStore>(),
                sp.GetRequiredService<ISettingsAccessor>());

            // ✅ start loading without blocking UI
            _ = vm.ActivateAsync(profileId?.Trim() ?? "", CancellationToken.None);

            return new GameInfoDialogContent
            {
                DataContext = vm
            };
        });

        // =========================================================
        // 14) MainWindow registration
        // =========================================================
        services.AddTransient<MainWindow>();

        // =========================================================
        // 15) Plugins discovery MUST be before final provider build
        // =========================================================
        IReadOnlyList<IGameModule> modules;
        using (var tempProvider = services.BuildServiceProvider())
        using (var tempScope = tempProvider.CreateScope())
        {
            modules = PluginsBootstrapper.DiscoverAndBootstrap(
                services,
                tempScope.ServiceProvider);
        }

        // =========================================================
        // 16) Build FINAL provider (includes plugin-added registrations)
        // =========================================================
        ServiceProvider = services.BuildServiceProvider();

        // =========================================================
        // 17) Register discovered modules into registry (final provider)
        // =========================================================
        PluginsBootstrapper.RegisterIntoRegistry(ServiceProvider, modules);

        // =========================================================
        // 18) Prime LogHub early (so UI shows logs from startup)
        // =========================================================
        _ = ServiceProvider.GetRequiredService<ILogHub>();
        MLogger.Instance.Info(LogChannel.SYSTEM, "[App] LogHub primed (UI live logs enabled).");

        // =========================================================
        // 18.5) Local Monitoring API (localhost only)
        // =========================================================
        try
        {
            _ = ServiceProvider.GetRequiredService<LocalMonitoringHost>()
                .StartAsync(5508, CancellationToken.None);
        }
        catch (Exception ex)
        {
            MLogger.Instance.Warn(LogChannel.SYSTEM, $"[App] LocalMonitoringHost failed to start: {ex.Message}");
        }

        // =========================================================
        // 19) Desktop lifetime boot (MUST set MainWindow)
        // =========================================================
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Graceful shutdown: flush archives and Serilog.
            desktop.Exit += (_, _) =>
            {
                try
                {
                    var archive = ServiceProvider?.GetService<LogSessionArchive>();
                    archive?.FlushToFile();
                    archive?.Dispose();
                }
                catch { /* ignore */ }

                // ✅ stop local api
                try
                {
                    var api = ServiceProvider?.GetService<LocalMonitoringHost>();
                    api?.StopAsync().GetAwaiter().GetResult();
                }
                catch { /* ignore */ }

                try { Log.CloseAndFlush(); } catch { /* ignore */ }
            };

            // Lock the app theme (no runtime switching)
            ApplyFixedTheme();

            // Create main window from DI.
            desktop.MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        }

        // Base must be called once, at the end.
        base.OnFrameworkInitializationCompleted();
    }

    private static void ApplyFixedTheme()
    {
        var theme = SukiTheme.GetInstance();
        theme.ChangeBaseTheme(ThemeVariant.Dark);

        // Try to select the purple theme used by the UI mock.
        var desired = theme.ColorThemes.FirstOrDefault(t =>
            string.Equals(t.DisplayName, "Purple", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.DisplayName, "Violet", StringComparison.OrdinalIgnoreCase) ||
            t.DisplayName.Contains("Purple", StringComparison.OrdinalIgnoreCase) ||
            t.DisplayName.Contains("Violet", StringComparison.OrdinalIgnoreCase));

        if (desired is not null)
            theme.ChangeColorTheme(desired);
    }
}
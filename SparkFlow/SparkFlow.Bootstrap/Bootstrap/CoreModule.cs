/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Bootstrap/CoreModule.cs
 * Purpose: Core DI bootstrap (LDPlayer-only).
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - LDPlayer is mandatory. No Disabled / Null / Multi-emulator fallback.
 *  - Windows-only by design.
 * ============================================================================ */

using SparkFlow.Infrastructure.Services.Devices;
using AdbLib.Abstractions;
using AdbLib.Options;
using AdbLib.Services;
using DeviceBindingLib.Extensions;
using EmulatorLib.Abstractions;
using EmulatorLib.DI;
using EmulatorLib.LDPlayer;
using GameModules.WarAndOrder.Module;
using GameModules.WarAndOrder.Ports;
using Microsoft.Extensions.DependencyInjection;
using SettingsStore;
using SettingsStore.Interfaces;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Emulator.AutoStart;
using SparkFlow.Abstractions.Services.Emulator.Binding;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using SparkFlow.Abstractions.Services.Health;
using SparkFlow.Abstractions.Services.Health.Abstractions;
using SparkFlow.Abstractions.Services.Logging;
using SparkFlow.Abstractions.State.Interfaces;
using SparkFlow.Engine.Engine.MultiAccount;
using SparkFlow.Engine.Engine.Scheduler;
using SparkFlow.Engine.Runner;
using SparkFlow.Engine.Services;
using SparkFlow.Infrastructure.Services.Accounts;
using SparkFlow.Infrastructure.Services.Emulator;
using SparkFlow.Infrastructure.Services.Emulator.AutoStart;
using SparkFlow.Infrastructure.Services.Emulator.Binding;
using SparkFlow.Infrastructure.Services.Emulator.Guards;
using SparkFlow.Infrastructure.Services.Game;
using SparkFlow.Infrastructure.Services.Game.Ports;
using SparkFlow.Infrastructure.Services.Health;
using SparkFlow.Infrastructure.Services.Health.Items;
using SparkFlow.Infrastructure.Services.Health.Storage;
using SparkFlow.Infrastructure.Services.Logging;
using SparkFlow.Infrastructure.Services.Monitoring;
using SparkFlow.Infrastructure.Services.Settings;
using UtiliLib;
using UtiliLib.Abstractions;
using UtiliLib.Net;
using UtiliLib.Options;
namespace SparkFlow.Bootstrap.Bootstrap;

public static class CoreModule
{
    public static IServiceCollection AddCoreServices(
        this IServiceCollection services,
        ISettingsAccessor settingsAccessor)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "SparkFlow requires Windows and LDPlayer to run.");

        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (settingsAccessor is null)
            throw new ArgumentNullException(nameof(settingsAccessor));

        // =========================================================
        // LOGGING
        // =========================================================
        services.AddSingleton(MLogger.Instance);
        services.AddSingleton<ILogHub>(_ => new LogHub(maxLogs: 3000));

        // =========================================================
        // SETTINGS
        // =========================================================
        // IMPORTANT: register accessor so HealthCheck items can read AppSettings safely
        services.AddSingleton(settingsAccessor);
        services.AddSingleton<SettingsAccessor>(_ => (SettingsAccessor)settingsAccessor);
        services.AddSingleton<ISettingsService>(_ =>
            new SettingsService(settingsAccessor));

        services.AddSingleton<IAppInfoService, AppInfoService>();

        // =========================================================
        // PORT SCANNER (ADB)
        // =========================================================
        services.AddSingleton(_ => new PortScannerOptions
        {
            AllowAggressiveReclaim = false,
            MaxRetries = 2,
            RetryDelayMs = 500,
            FreePortStart = 5000,
            FreePortEnd = 65000
        });

        services.AddSingleton<IPortScanner, PortScanner>();

        // =========================================================
        // ADB (Bundled + Runtime provisioning)
        // =========================================================
        services.AddSingleton(_ =>
        {
            var baseDir = AppContext.BaseDirectory;

            var bundledDir = Path.Combine(baseDir, "Assets", "platform-tools");
            var runtimeDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SparkFlow",
                "runtime",
                "platform-tools");

            var bundledVersion = "bundled";

            try
            {
                var props = Path.Combine(bundledDir, "source.properties");
                if (File.Exists(props))
                {
                    var line = File.ReadAllLines(props)
                        .FirstOrDefault(x =>
                            x.StartsWith("Pkg.Revision=", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(line))
                        bundledVersion = line.Split('=', 2)[1].Trim();
                }
            }
            catch
            {
                // Non-critical
            }

            return new AdbOptions
            {
                BundledPlatformToolsDir = bundledDir,
                RuntimePlatformToolsDir = runtimeDir,
                BundledVersion = bundledVersion,
                RestartServerOnProvision = true
            };
        });

        services.AddSingleton<IAdbProvisioning, AdbProvisioning>();
        services.AddSingleton<IAdbClient, AdbClient>();
        services.AddSingleton<IAdbMapping, AdbMapping>();

        // =========================================================
        // DEVICE BINDING
        // =========================================================
        services.AddDeviceBindingLib();

        // =========================================================
        // EMULATOR (LDPLAYER ONLY)
        // =========================================================
        services.AddEmulatorLib(); // Registers IEmulator -> LDPlayerEmulator (+ dnconsole discovery)

        // =========================================================
        // EMULATOR REQUIREMENTS (Core-owned, UI consumes Core only)
        // =========================================================
        services.AddSingleton<IEmulatorRequirementGuard, LDPlayerRequirementGuard>();
        services.AddSingleton<IPlatformRequirementGuard, LDPlayerRequirementGuardAdapter>();

        // =========================================================
        // DEVICE SESSIONS (ADB-first)
        // =========================================================
        services.AddSingleton<IDeviceSessionFactory, DeviceSessionFactory>();

        // =========================================================
        // GAME RUNNER + PORTS + MODULE
        // =========================================================
        services.AddSingleton<GameRunner>();
        services.AddSingleton<DeviceAutomationSelfTest>();

        services.AddSingleton<IDeviceAutomation, DeviceAutomationAdapter>();
        services.AddWarAndOrderModule();

        // =========================================================
        // PROFILES / ACCOUNTS
        // =========================================================
        services.AddSingleton<IProfilesStore, ProfilesStore>();
        services.AddSingleton<IAccountsSelector, AccountsSelector>();

        services.AddSingleton<ProfileValidationService>();
        services.AddSingleton<ProfileValidationWorker>();

        services.AddSingleton<ProfileDeviceBinder>();
        services.AddSingleton<ProfileDeviceResolver>();

        // =========================================================
        // AUTO-BIND
        // =========================================================
        services.AddSingleton<IProfilesAutoBinder, ProfilesAutoBinder>();

        // =========================================================
        // EMULATOR SERVICES (REAL IMPLEMENTATIONS)
        // =========================================================
        services.AddSingleton<IEmulatorAutoStarter, LDPlayerEmulatorAutoStarter>();
        services.AddSingleton<IEmulatorInstanceControlService, EmulatorInstanceControlService>();

        // =========================================================
        // HEALTH CHECK
        // =========================================================
        services.AddSingleton<IHealthReportStore, FileHealthReportStore>();

        // Step 0: open instance -> detect adb serial -> bind to profile -> stop instance
        services.AddSingleton<IHealthCheckItem, BindAdbSerialFromInstanceItem>();

        // Profile binding checks
        services.AddSingleton<IHealthCheckItem, AdbSerialPresentItem>();

        // Runtime checks
        services.AddSingleton<IHealthCheckItem, RuntimeFoldersItem>();
        services.AddSingleton<IHealthCheckItem, AdbRunningItem>();

        // Device checks (ADB)
        services.AddSingleton<IHealthCheckItem, AdbDeviceListedItem>();
        services.AddSingleton<IHealthCheckItem, AdbDeviceStateItem>();
        services.AddSingleton<IHealthCheckItem, AdbDeviceResponsiveItem>();

        // Machine checks
        services.AddSingleton<IHealthCheckItem, DeviceReadyItem>();

        // Automation checks
        services.AddSingleton<IHealthCheckItem, ScreenshotWorksItem>();
        services.AddSingleton<IHealthCheckItem, ResolutionOkItem>();

        // Game checks
        services.AddSingleton<IHealthCheckItem, WarAndOrderInstalledItem>();
        services.AddSingleton<IHealthCheckItem, WarAndOrderLaunchItem>();

        services.AddSingleton<HealthCheckRunner>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();

        // =========================================================
        // LOCAL MONITORING API (localhost only)
        // =========================================================
        services.AddSingleton<LocalMonitoringHost>();


        // =========================================================
        // ENGINE
        // =========================================================
        services.AddSingleton<IAccountQueue, AccountQueue>();
        services.AddSingleton<IRotationManager, RotationManager>();
        services.AddSingleton<IWaitLogic, WaitLogic>();
        services.AddSingleton<IResourceCheckScheduler, ResourceCheckScheduler>();
        services.AddSingleton<IInstanceSwitcher, InstanceSwitcher>();

        // =========================================================
        // GLOBAL RUNNER
        // =========================================================
        services.AddSingleton<IGlobalRunnerService, GlobalRunnerService>();
        // Low-level library registrations
        services.AddDeviceBindingLib();

        // Core services (single source of truth)
        services.AddSingleton<ProfileDeviceBinder>();
        services.AddSingleton<ProfileDeviceResolver>();
        return services;
    }
}

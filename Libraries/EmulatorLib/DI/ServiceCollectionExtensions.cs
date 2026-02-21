/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/EmulatorLib/DI/ServiceCollectionExtensions.cs
 * Purpose: EmulatorLib DI registration (LDPlayer-only).
 * Notes:
 *  - Registers LDPlayer as the only supported emulator.
 *  - Detects dnconsole.exe OR ldconsole.exe.
 * ============================================================================ */

using EmulatorLib.Abstractions;
using EmulatorLib.LDPlayer;

using Microsoft.Extensions.DependencyInjection;

namespace EmulatorLib.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmulatorLib(this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("LDPlayer requires Windows.");

        // Detect console exe (dnconsole.exe OR ldconsole.exe)
        var consoleExe = LDPlayerDiscovery.DetectConsoleExe();
        if (string.IsNullOrWhiteSpace(consoleExe))
            throw new InvalidOperationException(
                "LDPlayer is required but neither dnconsole.exe nor ldconsole.exe was found. " +
                "Install LDPlayer 9 or set LDPLAYER_HOME environment variable.");

        services.AddSingleton(_ => new LDPlayerCli(consoleExe));
        services.AddSingleton<LDPlayerTemplateService>();
        services.AddSingleton<IEmulator, LDPlayerEmulator>();

        return services;
    }
}

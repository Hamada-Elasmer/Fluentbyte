/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Module/WarAndOrderServiceCollectionExtensions.cs
 * Purpose: Library component: WarAndOrderServiceCollectionExtensions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Abstractions;
using GameContracts.Detection;
using GameContracts.Health;
using GameContracts.Lifecycle;
using GameContracts.Tasks;
using GameModules.WarAndOrder.Detection;
using GameModules.WarAndOrder.Health;
using GameModules.WarAndOrder.Lifecycle;
using GameModules.WarAndOrder.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GameModules.WarAndOrder.Module;

/// <summary>
/// DI registration for WarAndOrder module.
/// Call this from your App/CompositionRoot:
/// services.AddWarAndOrderModule();
/// </summary>
public static class WarAndOrderServiceCollectionExtensions
{
    public static IServiceCollection AddWarAndOrderModule(this IServiceCollection services)
    {
        // Core module services
        services.AddSingleton<IGameLifecycle, WarAndOrderLifecycle>();
        services.AddSingleton<IGameDetector, WarAndOrderDetector>();

        // Tasks (add all tasks you want available for this game)
        services.AddSingleton<IGameTask, CollectResourcesTask>();
        services.AddSingleton<IGameTask, UpgradeBuildingTask>();

        // Health checks (optional)
        services.AddSingleton<IGameHealthCheck, AdbConnectionHealthCheck>();

        // Finally, the module itself
        services.AddSingleton<IGameModule, WarAndOrderModule>();

        return services;
    }
}
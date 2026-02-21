/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Services/Plugins/PluginsBootstrapper.cs
 * Purpose: UI component: IGameRegistry.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using GameContracts.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace SparkFlow.UI.Services.Plugins;

// Local minimal definition to avoid IGameRegistry wiring errors (replace later with a real registry if available).
public interface IGameRegistry
{
    void Register(IGameModule module);
}

public static class PluginsBootstrapper
{
    /// <summary>
    /// 1) Discover plugin modules from ./games
    /// 2) For each module: call RegisterServices(IServiceCollection) OR Bootstrap(IServiceCollection) if found
    /// 3) Return discovered modules to be registered later in final provider registry
    /// </summary>
    public static IReadOnlyList<IGameModule> DiscoverAndBootstrap(IServiceCollection services, IServiceProvider tempProvider)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (tempProvider is null) throw new ArgumentNullException(nameof(tempProvider));

        var modules = new List<IGameModule>();

        var baseDir = AppContext.BaseDirectory;
        var gamesDir = Path.Combine(baseDir, "games");

        if (!Directory.Exists(gamesDir))
            return modules; // Must return the value.

        var dlls = Directory.EnumerateFiles(gamesDir, "*.dll", SearchOption.TopDirectoryOnly).ToList();
        if (dlls.Count == 0)
            return modules;

        foreach (var dll in dlls)
        {
            try
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);

                var gameModuleTypes = asm.GetTypes()
                    .Where(t =>
                        !t.IsAbstract &&
                        !t.IsInterface &&
                        typeof(IGameModule).IsAssignableFrom(t))
                    .ToList();

                foreach (var t in gameModuleTypes)
                {
                    IGameModule? module = null;

                    try
                    {
                        // If the module requires a DI constructor.
                        module = (IGameModule)ActivatorUtilities.CreateInstance(tempProvider, t);
                    }
                    catch
                    {
                        // Fallback to a parameterless constructor.
                        module = Activator.CreateInstance(t) as IGameModule;
                    }

                    if (module is null)
                        continue;

                    // Try to call RegisterServices(services) or Bootstrap(services) if present.
                    TryInvokeServicesHook(module, services);

                    modules.Add(module);
                }
            }
            catch
            {
                // Ignore the DLL if it fails to load.
                // (You can add logging here if desired.)
            }
        }

        return modules;
    }

    private static void TryInvokeServicesHook(IGameModule module, IServiceCollection services)
    {
        var type = module.GetType();

        // 1) RegisterServices(IServiceCollection)
        var m1 = type.GetMethod("RegisterServices", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(IServiceCollection) });
        if (m1 is not null)
        {
            m1.Invoke(module, new object[] { services });
            return;
        }

        // 2) Bootstrap(IServiceCollection)
        var m2 = type.GetMethod("Bootstrap", BindingFlags.Instance | BindingFlags.Public, new[] { typeof(IServiceCollection) });
        if (m2 is not null)
        {
            m2.Invoke(module, new object[] { services });
            return;
        }

        // 3) No-op if the module has no hooks.
    }

    /// <summary>
    /// After final provider is built, register discovered modules into registry.
    /// </summary>
    public static void RegisterIntoRegistry(IServiceProvider finalProvider, IReadOnlyList<IGameModule> modules)
    {
        if (finalProvider is null) throw new ArgumentNullException(nameof(finalProvider));
        if (modules is null) throw new ArgumentNullException(nameof(modules));

        // Get the registry (if not registered, fall back to a no-op).
        var registry = finalProvider.GetService<IGameRegistry>();
        if (registry is null)
            return;

        foreach (var m in modules)
            registry.Register(m);
    }
}
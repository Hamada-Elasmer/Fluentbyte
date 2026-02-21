/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Module/WarAndOrderModule.cs
 * Purpose: Library component: WarAndOrderModule.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Abstractions;
using GameContracts.Detection;
using GameContracts.Health;
using GameContracts.Lifecycle;
using GameContracts.Tasks;
using GameModules.WarAndOrder.Common;

namespace GameModules.WarAndOrder.Module;

/// <summary>
/// The "entry point" for the War and Order game module.
/// Core will discover it via DI as <see cref="IGameModule"/>.
/// </summary>
public sealed class WarAndOrderModule : IGameModule
{
    public string GameId => WarAndOrderConstants.GameId;

    public IGameLifecycle Lifecycle { get; }
    public IGameDetector Detector { get; }
    public IEnumerable<IGameTask> Tasks { get; }
    public IEnumerable<IGameHealthCheck> HealthChecks { get; }

    public WarAndOrderModule(
        IGameLifecycle lifecycle,
        IGameDetector detector,
        IEnumerable<IGameTask> tasks,
        IEnumerable<IGameHealthCheck> healthChecks)
    {
        Lifecycle = lifecycle;
        Detector = detector;
        Tasks = tasks;
        HealthChecks = healthChecks;
    }
}
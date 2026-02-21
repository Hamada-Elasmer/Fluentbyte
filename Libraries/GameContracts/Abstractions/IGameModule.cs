/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Abstractions/IGameModule.cs
 * Purpose: Library component: IGameModule.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Generic;
using GameContracts.Detection;
using GameContracts.Health;
using GameContracts.Lifecycle;
using GameContracts.Tasks;

namespace GameContracts.Abstractions;

public interface IGameModule
{
    string GameId { get; }
    IGameLifecycle Lifecycle { get; }
    IGameDetector Detector { get; }
    IEnumerable<IGameTask> Tasks { get; }
    IEnumerable<IGameHealthCheck> HealthChecks { get; }
}
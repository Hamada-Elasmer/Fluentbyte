/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Lifecycle/IGameLifecycle.cs
 * Purpose: Library component: IGameLifecycle.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Threading;
using System.Threading.Tasks;
using GameContracts.Common;

namespace GameContracts.Lifecycle;

public interface IGameLifecycle
{
    Task InstallAsync(GameContext context, CancellationToken ct);
    Task LaunchAsync(GameContext context, CancellationToken ct);
    Task WaitUntilReadyAsync(GameContext context, CancellationToken ct);
    Task ShutdownAsync(GameContext context, CancellationToken ct);
}
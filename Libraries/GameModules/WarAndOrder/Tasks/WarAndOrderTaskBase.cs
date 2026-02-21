/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Tasks/WarAndOrderTaskBase.cs
 * Purpose: Library component: for.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Common;
using GameContracts.Tasks;

namespace GameModules.WarAndOrder.Tasks;

/// <summary>
/// Base class for WarAndOrder tasks.
/// Use it to share helper methods and common patterns.
/// </summary>
public abstract class WarAndOrderTaskBase : IGameTask
{
    public abstract string TaskId { get; }

    public abstract Task<GameTaskResult> ExecuteAsync(GameContext context, CancellationToken ct);

    protected static GameTaskResult Ok(string message) => new(true, message);
    protected static GameTaskResult Fail(string message) => new(false, message);
}
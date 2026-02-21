/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Tasks/IGameTask.cs
 * Purpose: Library component: IGameTask.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Threading;
using System.Threading.Tasks;
using GameContracts.Common;

namespace GameContracts.Tasks;

public interface IGameTask
{
    string TaskId { get; }
    Task<GameTaskResult> ExecuteAsync(GameContext context, CancellationToken ct);
}
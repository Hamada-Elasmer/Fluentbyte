/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Health/IGameHealthCheck.cs
 * Purpose: Library component: IGameHealthCheck.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Threading;
using System.Threading.Tasks;
using GameContracts.Common;

namespace GameContracts.Health;

public interface IGameHealthCheck
{
    string CheckId { get; }

    /// <summary>
    /// Return null if OK, otherwise return an issue.
    /// </summary>
    Task<GameHealthIssue?> CheckAsync(GameContext context, CancellationToken ct);

    /// <summary>
    /// Try to fix automatically. Return true if fix likely succeeded.
    /// </summary>
    Task<bool> FixAsync(GameContext context, CancellationToken ct);
}
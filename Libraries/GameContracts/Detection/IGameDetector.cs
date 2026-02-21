/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Detection/IGameDetector.cs
 * Purpose: Library component: IGameDetector.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Threading;
using System.Threading.Tasks;
using GameContracts.Common;

namespace GameContracts.Detection;

public interface IGameDetector
{
    Task<bool> IsInstalledAsync(GameContext context, CancellationToken ct);
    Task<bool> IsMainScreenReadyAsync(GameContext context, CancellationToken ct);
    Task<bool> IsTutorialCompletedAsync(GameContext context, CancellationToken ct);
    Task<bool> HasBlockingDialogsAsync(GameContext context, CancellationToken ct);
}
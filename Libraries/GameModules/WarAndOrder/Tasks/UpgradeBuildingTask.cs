/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Tasks/UpgradeBuildingTask.cs
 * Purpose: Library component: UpgradeBuildingTask.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Common;
using GameContracts.Tasks;

namespace GameModules.WarAndOrder.Tasks;

/// <summary>
/// Example task: Upgrade a building.
/// 
/// In a real implementation you would:
/// - locate the target building (image match / coordinates / UI navigation)
/// - open building panel
/// - check upgrade requirements
/// - tap upgrade
/// - handle speedups (if allowed by your rules)
/// </summary>
public sealed class UpgradeBuildingTask : WarAndOrderTaskBase
{
    public override string TaskId => "upgrade_building";

    public override async Task<GameTaskResult> ExecuteAsync(GameContext context, CancellationToken ct)
    {
        // TODO: Implement real logic.
        await Task.Delay(250, ct);

        return Ok("UpgradeBuildingTask executed (placeholder).");
    }
}
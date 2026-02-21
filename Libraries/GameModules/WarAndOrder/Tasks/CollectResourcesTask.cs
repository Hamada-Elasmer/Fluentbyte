/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Tasks/CollectResourcesTask.cs
 * Purpose: Library component: CollectResourcesTask.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Common;
using GameContracts.Tasks;

namespace GameModules.WarAndOrder.Tasks;

/// <summary>
/// Example task: Collect resources.
/// 
/// NOTE:
/// This is only a scaffold.
/// Later you will implement the actual game interactions:
/// - navigate to resource buildings
/// - detect collect buttons
/// - tap
/// - verify result
/// </summary>
public sealed class CollectResourcesTask : WarAndOrderTaskBase
{
    public override string TaskId => "collect_resources";

    public override async Task<GameTaskResult> ExecuteAsync(GameContext context, CancellationToken ct)
    {
        // TODO: Implement real logic using Detection + DeviceAutomation.
        await Task.Delay(250, ct);

        return Ok("CollectResourcesTask executed (placeholder).");
    }
}
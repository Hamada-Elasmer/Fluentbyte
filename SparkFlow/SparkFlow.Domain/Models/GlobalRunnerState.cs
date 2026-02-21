/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Models/GlobalRunnerState.cs
 * Purpose: Core component: GlobalRunnerState.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Domain.Models;

public enum GlobalRunnerState
{
    Idle,
    Running,
    Paused,
    Stopping,
    Faulted,
    Unavailable
}
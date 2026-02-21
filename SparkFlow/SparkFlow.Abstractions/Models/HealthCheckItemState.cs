/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Models/HealthCheckItemState.cs
 * Purpose: Core component: HealthCheckItemState.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

public enum HealthCheckItemState
{
    Pending,
    Running,
    Ok,
    Warning,
    Error
}
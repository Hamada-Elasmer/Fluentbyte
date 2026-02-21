/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Models/Pages/HealthStatusEnum.cs
 * Purpose: Core component: HealthStatusEnum.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Domain.Models.Pages;

public enum HealthStatus
{
    Unknown,
    Ok,
    Warning,
    Error
}
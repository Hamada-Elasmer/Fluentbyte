/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Models/HealthIssueSeverity.cs
 * Purpose: Core component: HealthIssueSeverity.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// Severity order matters: Blocker is the highest priority.
/// </summary>
public enum HealthIssueSeverity
{
    Info = 0,
    Warning = 1,
    Blocker = 2
}
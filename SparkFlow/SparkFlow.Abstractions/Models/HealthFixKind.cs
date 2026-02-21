/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Models/HealthFixKind.cs
 * Purpose: Core component: HealthFixKind.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// FixKind indicates whether the issue can be fixed automatically or requires manual steps.
/// </summary>
public enum HealthFixKind
{
    None = 0,
    Manual = 1,
    Auto = 2
}
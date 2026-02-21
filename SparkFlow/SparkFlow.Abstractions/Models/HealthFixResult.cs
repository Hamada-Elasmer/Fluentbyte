/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Models/HealthFixResult.cs
 * Purpose: Core component: HealthFixResult.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// Result of applying fixes.
/// </summary>
public sealed class HealthFixResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";

    public int AppliedFixes { get; init; }
    public int SkippedManual { get; init; }
}
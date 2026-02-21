/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Models/HealthCheckItemResult.cs
 * Purpose: Core component: HealthCheckItemResult.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

public sealed class HealthCheckItemResult
{
    public HealthCheckItemId Id { get; init; }

    public string Title { get; init; } = "";

    public HealthCheckItemState State { get; init; } = HealthCheckItemState.Pending;

    // A short, user-friendly line.
    public string Message { get; init; } = "";

    // If we want to add an issue to the report.
    public HealthIssue? Issue { get; init; }

    // Should FixAll include this item?
    public bool HasAutoFix { get; init; }
}
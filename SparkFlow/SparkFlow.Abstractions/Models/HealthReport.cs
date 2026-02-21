/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Models/HealthReport.cs
 * Purpose: Core component: HealthReport.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Domain.Models.Pages;

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// Output of a health check run.
/// </summary>
public sealed class HealthReport
{
    public string ProfileId { get; init; } = "";

    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.Now;

    public HealthStatus Status { get; init; } = HealthStatus.Unknown;

    public List<HealthIssue> Issues { get; init; } = new();
}
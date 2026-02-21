/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Models/HealthIssue.cs
 * Purpose: Core component: HealthIssue.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// A single issue found by health checks.
/// </summary>
public sealed class HealthIssue
{
    /// <summary>
    /// The health check item that produced this issue.
    /// This is the PRIMARY stable mapping used by UI + FixAll logic.
    /// </summary>
    public HealthCheckItemId ItemId { get; init; }

    /// <summary>
    /// Stable identifier for grouping / analytics.
    /// Recommended format: health.{ItemId}.something
    /// </summary>
    public string Code { get; init; } = "";

    public string Title { get; init; } = "";
    public string Details { get; init; } = "";

    public HealthIssueSeverity Severity { get; init; } = HealthIssueSeverity.Info;
    public HealthFixKind FixKind { get; init; } = HealthFixKind.None;

    /// <summary>
    /// Optional short label for fix button (UI).
    /// Example: "Restart ADB", "Bind Serial", "Recheck".
    /// </summary>
    public string? FixTitle { get; init; }

    /// <summary>
    /// Optional manual steps shown to the user (if FixKind is Manual).
    /// </summary>
    public string? ManualSteps { get; init; }

    /// <summary>
    /// Optional timestamp for debugging (UTC).
    /// </summary>
    public DateTimeOffset? CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
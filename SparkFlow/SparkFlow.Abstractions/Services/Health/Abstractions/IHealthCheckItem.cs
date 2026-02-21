/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Health/Abstractions/IHealthCheckItem.cs
 * Purpose: Core component: IHealthCheckItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;

namespace SparkFlow.Abstractions.Services.Health.Abstractions;

/// <summary>
/// Single, fixed checklist item.
/// Runs ONE check only and reports ONE issue at most (or OK).
/// </summary>
public interface IHealthCheckItem
{
    HealthCheckItemId Id { get; }
    string Title { get; }

    /// <summary>
    /// Run the check. Return null => OK. Return issue => Warning/Error (based on Severity).
    /// </summary>
    Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default);

    /// <summary>
    /// Apply auto-fix if available for this item. Return true if something changed.
    /// Must be environment-only (no game actions).
    /// </summary>
    Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default);
}
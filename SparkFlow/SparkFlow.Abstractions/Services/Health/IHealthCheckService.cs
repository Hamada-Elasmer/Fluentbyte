/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Health/IHealthCheckService.cs
 * Purpose: Core component: IHealthCheckService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;

namespace SparkFlow.Abstractions.Services.Health;

/// <summary>
/// Provides environment checks and auto-fix pipeline per profile (Items-based).
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Force a live run (no cache guard). Use this for explicit user action (Run/Recheck).
    /// </summary>
    Task<HealthReport> RunAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Same as RunAsync but reports per-item row state live (Pending/Ok/Warning/Error).
    /// </summary>
    Task<HealthReport> RunLiveAsync(
        string profileId,
        IProgress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)> progress,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a cached recent report if available; otherwise runs live.
    /// Use this for Open (Auto-run with guard).
    /// </summary>
    Task<HealthReport> GetOrRunAsync(string profileId, TimeSpan? maxAge = null, CancellationToken ct = default);

    /// <summary>
    /// Cached guard version but also allows live progress when it does run.
    /// If cached report is returned, progress will NOT be replayed.
    /// </summary>
    Task<HealthReport> GetOrRunLiveAsync(
        string profileId,
        IProgress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)> progress,
        TimeSpan? maxAge = null,
        CancellationToken ct = default);

    /// <summary>
    /// Applies only auto-fixable fixes. Manual issues are skipped.
    /// Must re-check live at the end.
    /// </summary>
    Task<HealthFixResult> FixAllAutoAsync(string profileId, CancellationToken ct = default);
}
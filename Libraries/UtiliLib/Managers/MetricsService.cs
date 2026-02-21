/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Managers/MetricsService.cs
 * Purpose: Library component: MetricsService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using UtiliLib.Models;

namespace UtiliLib.Managers;

/// <summary>
/// Metrics collection service.
/// Note: The original implementation was not available in the source package.
/// This service is intentionally non-functional to avoid false assumptions.
/// </summary>
public sealed class MetricsService
{
    private static readonly Lazy<MetricsService> _lazyInstance =
        new(() => new MetricsService());

    /// <summary>
    /// Legacy singleton instance (kept for compatibility).
    /// Prefer DI and your own metrics pipeline.
    /// </summary>
    public static MetricsService Instance => _lazyInstance.Value;

    private MetricsService() { }

    public Task<MetricsEntry[]> GetEntries() =>
        Task.FromResult(Array.Empty<MetricsEntry>());

    public Task WriteToFileAsync(MetricsEntry[] entries) =>
        throw new NotSupportedException("MetricsService is a stub in this package. Provide a real metrics implementation.");

    public void ProcessQueue() =>
        throw new NotSupportedException("MetricsService is a stub in this package. Provide a real metrics implementation.");
}
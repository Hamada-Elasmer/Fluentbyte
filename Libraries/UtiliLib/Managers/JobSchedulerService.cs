/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Managers/JobSchedulerService.cs
 * Purpose: Library component: JobSchedulerService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using UtiliLib.Models;

namespace UtiliLib.Managers;

/// <summary>
/// Manager for scheduling and running jobs.
/// Note: The original implementation was not available in the source package.
/// This service is intentionally non-functional to avoid false assumptions.
/// </summary>
public sealed class JobSchedulerService
{
    private CancellationTokenSource _cancellationTokenSource = new();

    private static readonly Lazy<JobSchedulerService> _lazyInstance =
        new(() => new JobSchedulerService());

    /// <summary>
    /// Legacy singleton instance (kept for compatibility).
    /// Prefer DI and your own scheduler implementation.
    /// </summary>
    public static JobSchedulerService Instance => _lazyInstance.Value;

    public Task Run(ScheduledJob job, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("JobSchedulerService is a stub in this package. Provide a real scheduler implementation.");
    }

    public Task Run(List<ScheduledJob> jobs, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("JobSchedulerService is a stub in this package. Provide a real scheduler implementation.");
    }

    public void Cancel()
    {
        try { _cancellationTokenSource.Cancel(); } catch { /* ignore */ }
        _cancellationTokenSource = new CancellationTokenSource();
    }
}
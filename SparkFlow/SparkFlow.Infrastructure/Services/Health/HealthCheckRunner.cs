/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/HealthCheckRunner.cs
 * Purpose: Core component: HealthCheckRunner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Diagnostics;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;
using SparkFlow.Domain.Models.Pages;
using SparkFlow.Infrastructure.Services.Health.Storage;

namespace SparkFlow.Infrastructure.Services.Health;

public sealed class HealthCheckRunner
{
    private readonly IReadOnlyList<IHealthCheckItem> _items;
    private readonly IHealthReportStore _store;

    // Fixed UI order (NOT enum order).
    // NOTE: Add new items here to control the UI order.
    //
    // IMPORTANT:
    // - This array is also used as the "UI whitelist".
    // - Only items listed here will be visible in UI AND executed by the runner.
    private static readonly HealthCheckItemId[] UiOrder =
    {
        // Runtime
        HealthCheckItemId.RuntimeFolders,
        HealthCheckItemId.AdbRunning,

        // Profile binding (MUST be first before device checks)
        HealthCheckItemId.BindAdbSerialFromInstance,
        HealthCheckItemId.Adb_SerialPresent,
    };

    // Fast lookup for filtering items to ONLY the UI-visible subset.
    private static readonly HashSet<HealthCheckItemId> UiVisible = new(UiOrder);

    /// <summary>
    /// Ordered items by fixed UI order (stable for UI + FixAll).
    /// IMPORTANT: Items are FILTERED to UiOrder only (UI whitelist).
    /// </summary>
    public IEnumerable<IHealthCheckItem> ItemsOrdered
        => OrderByUi(_items)
            .Where(x => UiVisible.Contains(x.Id));

    public HealthCheckRunner(IEnumerable<IHealthCheckItem> items, IHealthReportStore store)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        _items = items.ToList();
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Runs items checks (live) and reports row states to UI.
    /// We report: Pending -> Running -> (Ok/Warning/Error) per row.
    /// Also includes timing to detect slow checks.
    /// </summary>
    public async Task<HealthReport> RunLiveAsync(
        HealthCheckContext ctx,
        IProgress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)> progress,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx is null) throw new ArgumentNullException(nameof(ctx));
        if (string.IsNullOrWhiteSpace(ctx.ProfileId))
            throw new ArgumentException("ProfileId is required.", nameof(ctx));

        var ordered = ItemsOrdered.ToList();

        // Collect final issues only (report will persist to disk).
        var issues = new List<HealthIssue>();

        // UI: initialize all rows to Pending (once)
        foreach (var it in ordered)
            progress.Report((it.Id, HealthCheckItemState.Pending, "", null));

        foreach (var it in ordered)
        {
            ct.ThrowIfCancellationRequested();

            // UI: show "Running" while this item is being checked
            progress.Report((it.Id, HealthCheckItemState.Running, "Running...", null));

            var sw = Stopwatch.StartNew();
            HealthIssue? issue = null;

            try
            {
                issue = await it.CheckAsync(ctx, ct);
            }
            catch (Exception ex)
            {
                issue = new HealthIssue
                {
                    Code = $"health.{it.Id}.exception",
                    Title = $"{it.Title} failed",
                    Details = ex.Message,
                    Severity = HealthIssueSeverity.Blocker,
                    FixKind = HealthFixKind.None
                };
            }
            finally
            {
                sw.Stop();
            }

            var tookMs = sw.ElapsedMilliseconds;
            var tookMsg = tookMs >= 800 ? $" (took {tookMs} ms)" : "";

            if (issue is null)
            {
                progress.Report((it.Id, HealthCheckItemState.Ok, $"OK{tookMsg}", null));
                continue;
            }

            issues.Add(issue);

            var state = issue.Severity switch
            {
                HealthIssueSeverity.Blocker => HealthCheckItemState.Error,
                HealthIssueSeverity.Warning => HealthCheckItemState.Warning,
                _ => HealthCheckItemState.Warning
            };

            progress.Report((it.Id, state, $"{issue.Title}{tookMsg}", issue));
        }

        var report = new HealthReport
        {
            ProfileId = ctx.ProfileId,
            CheckedAt = DateTimeOffset.Now,
            Issues = issues
                .OrderByDescending(i => i.Severity)
                .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Status = ComputeStatus(issues)
        };

        await _store.SaveLastAsync(report, ct);

        return report;
    }

    private static IEnumerable<IHealthCheckItem> OrderByUi(IEnumerable<IHealthCheckItem> items)
    {
        var rank = new Dictionary<HealthCheckItemId, int>();
        for (var i = 0; i < UiOrder.Length; i++)
            rank[UiOrder[i]] = i;

        return items
            .OrderBy(x => rank.TryGetValue(x.Id, out var r) ? r : int.MaxValue)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase);
    }

    private static HealthStatus ComputeStatus(List<HealthIssue> issues)
    {
        if (issues.Any(i => i.Severity == HealthIssueSeverity.Blocker))
            return HealthStatus.Error;

        if (issues.Any(i => i.Severity == HealthIssueSeverity.Warning))
            return HealthStatus.Warning;

        return HealthStatus.Ok;
    }
}

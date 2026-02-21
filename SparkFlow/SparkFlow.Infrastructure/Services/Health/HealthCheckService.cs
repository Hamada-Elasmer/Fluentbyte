/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/HealthCheckService.cs
 * Purpose: Core component: HealthCheckService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using AdbLib.Options;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Health;
using SparkFlow.Domain.Models.Pages;
using SparkFlow.Infrastructure.Services.Health.Storage;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Health;

public sealed class HealthCheckService : IHealthCheckService
{
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(2);

    private readonly MLogger _log;
    private readonly IProfilesStore _profiles;
    private readonly HealthCheckRunner _runner;
    private readonly IHealthReportStore _store;
    private readonly AdbOptions _adbOptions;

    private readonly Dictionary<string, HealthReport> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public HealthCheckService(
        MLogger logger,
        IProfilesStore profiles,
        HealthCheckRunner runner,
        IHealthReportStore store,
        AdbOptions adbOptions)
    {
        _log = logger ?? MLogger.Instance;
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _adbOptions = adbOptions ?? throw new ArgumentNullException(nameof(adbOptions));
    }

    public Task<HealthReport> GetOrRunAsync(
        string profileId,
        TimeSpan? maxAge = null,
        CancellationToken ct = default)
        => GetOrRunInternalAsync(
            profileId,
            forceLive: false,
            maxAge: maxAge ?? DefaultMaxAge,
            progress: null,
            ct: ct);

    public Task<HealthReport> RunAsync(string profileId, CancellationToken ct = default)
        => GetOrRunInternalAsync(
            profileId,
            forceLive: true,
            maxAge: TimeSpan.Zero,
            progress: null,
            ct: ct);

    public Task<HealthReport> RunLiveAsync(
        string profileId,
        IProgress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)> progress,
        CancellationToken ct = default)
        => GetOrRunInternalAsync(
            profileId,
            forceLive: true,
            maxAge: TimeSpan.Zero,
            progress: progress,
            ct: ct);

    public Task<HealthReport> GetOrRunLiveAsync(
        string profileId,
        IProgress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)> progress,
        TimeSpan? maxAge = null,
        CancellationToken ct = default)
        => GetOrRunInternalAsync(
            profileId,
            forceLive: false,
            maxAge: maxAge ?? DefaultMaxAge,
            progress: progress,
            ct: ct);

    public async Task<HealthFixResult> FixAllAutoAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(profileId))
        {
            return new HealthFixResult
            {
                Success = false,
                Message = "ProfileId is empty.",
                AppliedFixes = 0,
                SkippedManual = 0
            };
        }

        profileId = profileId.Trim();

        _log.Info(LogChannel.SYSTEM, $"[HealthCheck] FixAllAutoAsync started (ProfileId='{profileId}').");

        // 1) Get latest report (cached ok)
        var before = await GetOrRunAsync(profileId, maxAge: DefaultMaxAge, ct).ConfigureAwait(false);

        // Abort only on fatal context errors
        if (HasFatalContextIssue(before))
        {
            _log.Error(LogChannel.SYSTEM,
                $"[HealthCheck] FixAllAutoAsync aborted: fatal context issue (ProfileId='{profileId}').");

            return new HealthFixResult
            {
                Success = false,
                Message = "Cannot apply FixAll because profile context is missing/invalid.",
                AppliedFixes = 0,
                SkippedManual = before.Issues.Count(i => i.FixKind == HealthFixKind.Manual)
            };
        }

        var manualCount = before.Issues.Count(i => i.FixKind == HealthFixKind.Manual);
        var autoIssues = before.Issues.Where(i => i.FixKind == HealthFixKind.Auto).ToList();

        if (autoIssues.Count == 0)
        {
            _log.Info(LogChannel.SYSTEM, "[HealthCheck] FixAllAutoAsync: no auto-fixable issues.");

            return new HealthFixResult
            {
                Success = true,
                Message = manualCount > 0
                    ? "No auto fixes available (manual issues exist)."
                    : "No fixes available.",
                AppliedFixes = 0,
                SkippedManual = manualCount
            };
        }

        // 2) Context once
        HealthCheckContext ctx;
        try
        {
            ctx = await BuildContextAsync(profileId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex,
                $"[HealthCheck] FixAllAutoAsync aborted: context build failed (ProfileId='{profileId}').");

            return new HealthFixResult
            {
                Success = false,
                Message = "Cannot apply FixAll because context build failed (missing/invalid profile context).",
                AppliedFixes = 0,
                SkippedManual = manualCount
            };
        }

        // 3) Map auto issues -> itemIds (only fix those)
        var autoItemIds = autoIssues
            .Select(MapIssueToItemId)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .ToHashSet();

        var attempted = 0;
        var applied = 0;

        foreach (var item in _runner.ItemsOrdered)
        {
            ct.ThrowIfCancellationRequested();

            if (!autoItemIds.Contains(item.Id))
                continue;

            attempted++;

            try
            {
                var changed = await item.TryAutoFixAsync(ctx, ct).ConfigureAwait(false);
                if (changed) applied++;

                _log.Info(LogChannel.SYSTEM,
                    $"[HealthCheck] AutoFix: {item.Id} ({item.Title}) Changed={changed}.");
            }
            catch (Exception ex)
            {
                _log.Exception(LogChannel.SYSTEM, ex,
                    $"[HealthCheck] AutoFix failed for {item.Id} ({item.Title}).");
            }
        }

        // 4) Recheck live
        var after = await RunAsync(profileId, ct).ConfigureAwait(false);

        var beforeAutoLeft = before.Issues.Count(i => i.FixKind == HealthFixKind.Auto);
        var afterAutoLeft = after.Issues.Count(i => i.FixKind == HealthFixKind.Auto);

        var improved =
            after.Status == HealthStatus.Ok
            || afterAutoLeft < beforeAutoLeft
            || after.Status != before.Status;

        var msg =
            improved
                ? $"FixAll finished. Auto issues: {beforeAutoLeft} -> {afterAutoLeft} (attempted {attempted}, applied {applied})."
                : $"Tried auto fixes but nothing changed (attempted {attempted}, applied {applied}).";

        _log.Info(LogChannel.SYSTEM,
            $"[HealthCheck] FixAllAutoAsync finished. Attempted={attempted}, Applied={applied}, BeforeAuto={beforeAutoLeft}, AfterAuto={afterAutoLeft}, Manual={manualCount}, AfterStatus={after.Status}.");

        return new HealthFixResult
        {
            Success = true,
            Message = msg,
            AppliedFixes = applied,
            SkippedManual = manualCount
        };
    }

    private static bool HasFatalContextIssue(HealthReport report)
    {
        // These are the ONLY cases where FixAll should abort.
        return report.Issues.Any(i =>
            string.Equals(i.Code, "health.profile.missing", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(i.Code, "health.profile.not_found", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<HealthReport> GetOrRunInternalAsync(
        string profileId,
        bool forceLive,
        TimeSpan maxAge,
        IProgress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)>? progress,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(profileId))
        {
            return new HealthReport
            {
                ProfileId = "",
                Status = HealthStatus.Error,
                Issues = new List<HealthIssue>
                {
                    new()
                    {
                        Code = "health.profile.missing",
                        Title = "No profile selected",
                        Details = "ProfileId is empty.",
                        Severity = HealthIssueSeverity.Blocker,
                        FixKind = HealthFixKind.None
                    }
                }
            };
        }

        profileId = profileId.Trim();

        // 1) memory cache
        if (!forceLive && _cache.TryGetValue(profileId, out var cached))
        {
            var age = DateTimeOffset.Now - cached.CheckedAt;
            if (age <= maxAge)
                return cached;
        }

        // 2) disk cache
        if (!forceLive)
        {
            var last = await _store.LoadLastAsync(profileId, ct).ConfigureAwait(false);
            if (last is not null)
            {
                _cache[profileId] = last;

                var age = DateTimeOffset.Now - last.CheckedAt;
                if (age <= maxAge)
                    return last;
            }
        }

        _log.Info(LogChannel.SYSTEM, $"[HealthCheck] Run live (ProfileId='{profileId}', Force={forceLive}).");

        HealthCheckContext ctx;
        try
        {
            ctx = await BuildContextAsync(profileId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.Exception(LogChannel.SYSTEM, ex,
                $"[HealthCheck] Context build failed (ProfileId='{profileId}').");

            return new HealthReport
            {
                ProfileId = profileId,
                Status = HealthStatus.Error,
                Issues = new List<HealthIssue>
                {
                    new()
                    {
                        Code = "health.profile.not_found",
                        Title = "Profile not found",
                        Details = $"ProfileId '{profileId}' does not exist in ProfilesStore.",
                        Severity = HealthIssueSeverity.Blocker,
                        FixKind = HealthFixKind.None
                    }
                }
            };
        }

        // If there is no progress callback, send a no-op update.
        progress ??= new Progress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)>(_ => { });

        var report = await _runner.RunLiveAsync(ctx, progress, ct).ConfigureAwait(false);

        _cache[profileId] = report;

        // Runner already persists report, so do NOT double-save here.
        return report;
    }

    private async Task<HealthCheckContext> BuildContextAsync(string profileId, CancellationToken ct)
    {
        var profile = await _profiles.GetByIdAsync(profileId, ct).ConfigureAwait(false);

        if (profile is null)
        {
            _log.Error(LogChannel.SYSTEM,
                $"[HealthCheck] BuildContextAsync failed: profile not found (ProfileId='{profileId}').");

            throw new InvalidOperationException($"Profile not found: '{profileId}'.");
        }

        return new HealthCheckContext
        {
            ProfileId = profileId,
            AdbOptions = _adbOptions,
            InstanceId = string.IsNullOrWhiteSpace(profile.InstanceId) ? null : profile.InstanceId.Trim(),
            AdbSerial = string.IsNullOrWhiteSpace(profile.AdbSerial) ? null : profile.AdbSerial.Trim()
        };
    }

    private static HealthCheckItemId? MapIssueToItemId(HealthIssue issue)
    {
        if (string.IsNullOrWhiteSpace(issue.Code))
            return null;

        var code = issue.Code.Trim();
        if (!code.StartsWith("health.", StringComparison.OrdinalIgnoreCase))
            return null;

        // Expected: health.{ItemId}.something
        var parts = code.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        if (Enum.TryParse(parts[1], ignoreCase: true, out HealthCheckItemId id))
            return id;

        return null;
    }
}

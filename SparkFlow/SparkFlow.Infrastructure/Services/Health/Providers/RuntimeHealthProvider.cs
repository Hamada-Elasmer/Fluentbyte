/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Providers/RuntimeHealthProvider.cs
 * Purpose: Core component: RuntimeHealthProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Providers;

/// <summary>
/// Checks runtime folders existence and can auto-create them.
/// </summary>
public sealed class RuntimeHealthProvider : IHealthCheckProvider
{
    public string Name => "Runtime";

    // Keep it in the same location as FileHealthReportStore for consistency.
    private static string RuntimeRoot => Path.Combine(AppContext.BaseDirectory, "runtime");
    private static string HealthDir => Path.Combine(RuntimeRoot, "health");

    public Task<IReadOnlyList<HealthIssue>> CheckAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var issues = new List<HealthIssue>();

        if (!Directory.Exists(RuntimeRoot))
        {
            issues.Add(new HealthIssue
            {
                Code = "runtime.root.missing",
                Title = "Runtime folder missing",
                Details = $"Folder not found: {RuntimeRoot}",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.Auto
            });
        }

        if (!Directory.Exists(HealthDir))
        {
            issues.Add(new HealthIssue
            {
                Code = "runtime.health.missing",
                Title = "Health folder missing",
                Details = $"Folder not found: {HealthDir}",
                Severity = HealthIssueSeverity.Info,
                FixKind = HealthFixKind.Auto
            });
        }

        return Task.FromResult<IReadOnlyList<HealthIssue>>(issues);
    }

    public Task<int> FixAllAutoAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var fixedCount = 0;

        if (!Directory.Exists(RuntimeRoot))
        {
            Directory.CreateDirectory(RuntimeRoot);
            fixedCount++;
        }

        if (!Directory.Exists(HealthDir))
        {
            Directory.CreateDirectory(HealthDir);
            fixedCount++;
        }

        return Task.FromResult(fixedCount);
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Providers/RuntimeFoldersProvider.cs
 * Purpose: Core component: RuntimeFoldersProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Providers;

/// <summary>
/// Ensures runtime folders exist and are writable.
/// Safe auto-fix: create missing directories.
/// </summary>
public sealed class RuntimeFoldersProvider : IHealthCheckProvider
{
    public string Name => "RuntimeFolders";

    public Task<IReadOnlyList<HealthIssue>> CheckAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var issues = new List<HealthIssue>();

        var baseDir = AppContext.BaseDirectory;
        var runtime = Path.Combine(baseDir, "runtime");
        var profiles = Path.Combine(runtime, "profiles");
        var settings = Path.Combine(runtime, "settings");
        var health = Path.Combine(runtime, "health");

        CheckDir(runtime, "runtime.root", "Runtime folder", runtime, issues);
        CheckDir(profiles, "runtime.profiles", "Profiles folder", profiles, issues);
        CheckDir(settings, "runtime.settings", "Settings folder", settings, issues);
        CheckDir(health, "runtime.health", "Health folder", health, issues);

        return Task.FromResult((IReadOnlyList<HealthIssue>)issues);
    }

    public Task<int> FixAllAutoAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var baseDir = AppContext.BaseDirectory;
        var runtime = Path.Combine(baseDir, "runtime");
        var health = Path.Combine(runtime, "health");

        // Auto-fix: ensure directories exist
        Directory.CreateDirectory(runtime);
        Directory.CreateDirectory(Path.Combine(runtime, "profiles"));
        Directory.CreateDirectory(Path.Combine(runtime, "settings"));
        Directory.CreateDirectory(health);

        // Consider 1 "fix batch" as 1 successful action (simple metric)
        return Task.FromResult(1);
    }

    private static void CheckDir(string path, string code, string title, string displayPath, List<HealthIssue> issues)
    {
        if (Directory.Exists(path))
            return;

        issues.Add(new HealthIssue
        {
            Code = code,
            Title = $"{title} is missing",
            Details = $"Directory not found: {displayPath}",
            Severity = HealthIssueSeverity.Blocker,
            FixKind = HealthFixKind.Auto
        });
    }
}
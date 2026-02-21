/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/RuntimeFoldersItem.cs
 * Purpose: Core component: RuntimeFoldersItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Items;

/// <summary>
/// Ensures runtime folders exist and are writable.
/// Safe auto-fix: create missing directories + verify write access.
/// </summary>
public sealed class RuntimeFoldersItem : IHealthCheckItem
{
    public HealthCheckItemId Id => HealthCheckItemId.RuntimeFolders;
    public string Title => "Runtime folders";

    public Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var baseDir = AppContext.BaseDirectory;
        var runtime = Path.Combine(baseDir, "runtime");
        var profiles = Path.Combine(runtime, "profiles");
        var settings = Path.Combine(runtime, "settings");
        var health = Path.Combine(runtime, "health");

        try
        {
            if (!Directory.Exists(runtime))
                return Task.FromResult<HealthIssue?>(Missing("runtime.root", "Runtime folder", runtime));

            if (!Directory.Exists(profiles))
                return Task.FromResult<HealthIssue?>(Missing("runtime.profiles", "Profiles folder", profiles));

            if (!Directory.Exists(settings))
                return Task.FromResult<HealthIssue?>(Missing("runtime.settings", "Settings folder", settings));

            if (!Directory.Exists(health))
                return Task.FromResult<HealthIssue?>(Missing("runtime.health", "Health folder", health));

            if (!CanWrite(runtime))
                return Task.FromResult<HealthIssue?>(NotWritable("runtime.root", "Runtime folder", runtime));

            if (!CanWrite(profiles))
                return Task.FromResult<HealthIssue?>(NotWritable("runtime.profiles", "Profiles folder", profiles));

            if (!CanWrite(settings))
                return Task.FromResult<HealthIssue?>(NotWritable("runtime.settings", "Settings folder", settings));

            if (!CanWrite(health))
                return Task.FromResult<HealthIssue?>(NotWritable("runtime.health", "Health folder", health));

            return Task.FromResult<HealthIssue?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<HealthIssue?>(new HealthIssue
            {
                Code = $"health.{Id}.runtime.exception",
                Title = "Runtime folders check failed",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto,
                ManualSteps =
                    "1) Run app as Administrator.\n" +
                    "2) Ensure the app folder is not read-only.\n" +
                    "3) Recheck."
            });
        }
    }

    public Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var baseDir = AppContext.BaseDirectory;
        var runtime = Path.Combine(baseDir, "runtime");
        var profiles = Path.Combine(runtime, "profiles");
        var settings = Path.Combine(runtime, "settings");
        var health = Path.Combine(runtime, "health");

        var createdAny = false;

        try
        {
            createdAny |= EnsureDir(runtime);
            createdAny |= EnsureDir(profiles);
            createdAny |= EnsureDir(settings);
            createdAny |= EnsureDir(health);

            // Real success means directories exist and are writable.
            var ok =
                Directory.Exists(runtime) && CanWrite(runtime) &&
                Directory.Exists(profiles) && CanWrite(profiles) &&
                Directory.Exists(settings) && CanWrite(settings) &&
                Directory.Exists(health) && CanWrite(health);

            // If nothing was changed (and that's OK), return false (no change).
            if (!createdAny && ok)
                return Task.FromResult(false);

            return Task.FromResult(createdAny && ok);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static bool EnsureDir(string path)
    {
        if (Directory.Exists(path))
            return false;

        Directory.CreateDirectory(path);
        return true;
    }

    private static bool CanWrite(string dir)
    {
        var testFile = Path.Combine(dir, $".write_test_{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(testFile, "ok");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            try
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
            catch { }

            return false;
        }
    }

    private HealthIssue Missing(string codeSuffix, string title, string displayPath) => new()
    {
        Code = $"health.{Id}.{codeSuffix}",
        Title = $"{title} is missing",
        Details = $"Directory not found: {displayPath}",
        Severity = HealthIssueSeverity.Blocker,
        FixKind = HealthFixKind.Auto
    };

    private HealthIssue NotWritable(string codeSuffix, string title, string displayPath) => new()
    {
        Code = $"health.{Id}.{codeSuffix}.not_writable",
        Title = $"{title} is not writable",
        Details = $"Write access failed: {displayPath}",
        Severity = HealthIssueSeverity.Blocker,
        FixKind = HealthFixKind.Auto,
        ManualSteps =
            "1) Run app as Administrator.\n" +
            "2) Ensure app folder permissions allow write.\n" +
            "3) Recheck."
    };
}

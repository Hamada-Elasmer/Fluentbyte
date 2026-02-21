/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Storage/FileHealthReportStore.cs
 * Purpose: Core component: FileHealthReportStore.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Text.Json;
using SparkFlow.Abstractions.Models;

namespace SparkFlow.Infrastructure.Services.Health.Storage;

/// <summary>
/// Simple file store under runtime/health.
/// Keeps last report for each profile.
/// </summary>
public sealed class FileHealthReportStore : IHealthReportStore
{
    private readonly string _dir;

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        WriteIndented = true
    };

    public FileHealthReportStore()
    {
        _dir = Path.Combine(AppContext.BaseDirectory, "runtime", "health");
        Directory.CreateDirectory(_dir);
    }

    public async Task<HealthReport?> LoadLastAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(profileId)) return null;

        var path = Path.Combine(_dir, $"{profileId}.json");
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<HealthReport>(json, JsonOpt);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveLastAsync(HealthReport report, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (report is null) throw new ArgumentNullException(nameof(report));
        if (string.IsNullOrWhiteSpace(report.ProfileId)) throw new ArgumentException("Report.ProfileId is required.");

        Directory.CreateDirectory(_dir);

        var json = JsonSerializer.Serialize(report, JsonOpt);
        var path = Path.Combine(_dir, $"{report.ProfileId}.json");
        var tmp = path + ".tmp";

        await File.WriteAllTextAsync(tmp, json, ct);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tmp, path);
    }
}
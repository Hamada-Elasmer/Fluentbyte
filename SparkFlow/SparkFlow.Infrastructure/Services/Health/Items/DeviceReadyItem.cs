/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/DeviceReadyItem.cs
 * Purpose: Core component: DeviceReadyItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Runtime.InteropServices;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class DeviceReadyItem : IHealthCheckItem
{
    public HealthCheckItemId Id => HealthCheckItemId.DeviceReady;
    public string Title => "Device Ready";

    public Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Disk check (real)
        var baseDir = AppContext.BaseDirectory;
        var root = Path.GetPathRoot(baseDir) ?? baseDir;
        var drive = new DriveInfo(root);
        var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);

        if (freeGb < 5)
        {
            return Task.FromResult<HealthIssue?>(new HealthIssue
            {
                Code = $"health.{Id}.disk_low",
                Title = "Low disk space",
                Details = $"Free space is {freeGb:0.0} GB. Recommended >= 5 GB.",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.Manual,
                ManualSteps = "Free some disk space then recheck."
            });
        }

        // RAM check (Windows real via kernel32)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var totalGb = GetTotalPhysicalMemoryGb();
            if (totalGb > 0 && totalGb < 4)
            {
                return Task.FromResult<HealthIssue?>(new HealthIssue
                {
                    Code = $"health.{Id}.ram_low",
                    Title = "Low RAM",
                    Details = $"Installed RAM is {totalGb:0.0} GB. Recommended >= 4 GB.",
                    Severity = HealthIssueSeverity.Warning,
                    FixKind = HealthFixKind.Manual,
                    ManualSteps = "Close heavy apps or upgrade RAM."
                });
            }
        }

        return Task.FromResult<HealthIssue?>(null);
    }

    public Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
        => Task.FromResult(false);

    private static double GetTotalPhysicalMemoryGb()
    {
        try
        {
            if (GetPhysicallyInstalledSystemMemory(out var kb))
            {
                var bytes = kb * 1024.0;
                return bytes / (1024 * 1024 * 1024);
            }
        }
        catch { }
        return 0;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetPhysicallyInstalledSystemMemory(out long totalMemoryInKilobytes);
}
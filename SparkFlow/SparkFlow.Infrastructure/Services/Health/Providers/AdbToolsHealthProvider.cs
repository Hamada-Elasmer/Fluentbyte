/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Providers/AdbToolsHealthProvider.cs
 * Purpose: Core component: AdbToolsHealthProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Diagnostics;
using AdbLib.Abstractions;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Providers;

/// <summary>
/// ADB tools health provider (Owner ADB).
///
/// Goals:
/// - Ensure SparkFlow's bundled ADB is provisioned to runtime (single source of truth).
/// - Verify adb.exe can run.
/// - Verify adb server can start.
///
/// Notes:
/// - We do NOT require a connected device here. "No devices" is a WARNING (depends on emulator state).
/// - Auto-fix: kill-server / start-server using the provisioned Owner ADB.
/// </summary>
public sealed class AdbToolsHealthProvider : IHealthCheckProvider
{
    public string Name => "ADB";

    private readonly IAdbProvisioning _provisioning;

    public AdbToolsHealthProvider(IAdbProvisioning provisioning)
    {
        _provisioning = provisioning ?? throw new ArgumentNullException(nameof(provisioning));
    }

    public async Task<IReadOnlyList<HealthIssue>> CheckAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var issues = new List<HealthIssue>();

        // 1) Ensure Owner ADB exists (auto-provision from bundled -> runtime)
        string adbExe;
        try
        {
            adbExe = _provisioning.EnsureProvisioned();
        }
        catch (Exception ex)
        {
            issues.Add(new HealthIssue
            {
                Code = "adb.provision.failed",
                Title = "ADB provisioning failed",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto,
                ManualSteps =
                    "1) Ensure Assets/platform-tools exists.\n" +
                    "2) Ensure adb.exe is present.\n" +
                    "3) Re-run SparkFlow."
            });

            return issues;
        }

        // 2) adb version
        var ver = await RunProcessAsync(adbExe, "version", ct);
        if (ver.ExitCode != 0)
        {
            issues.Add(new HealthIssue
            {
                Code = "adb.version.failed",
                Title = "ADB cannot run",
                Details = $"adb version failed. {ver.ErrorOrOutput}",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            });

            return issues;
        }

        // 3) start-server
        var start = await RunProcessAsync(adbExe, "start-server", ct);
        if (start.ExitCode != 0)
        {
            issues.Add(new HealthIssue
            {
                Code = "adb.server.failed",
                Title = "ADB server failed",
                Details = start.ErrorOrOutput,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            });

            return issues;
        }

        // 4) adb devices (sanity)
        var dev = await RunProcessAsync(adbExe, "devices -l", ct);
        if (dev.ExitCode != 0)
        {
            issues.Add(new HealthIssue
            {
                Code = "adb.devices.failed",
                Title = "ADB devices failed",
                Details = dev.ErrorOrOutput,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            });

            return issues;
        }

        // If no devices -> warning (emulator may be closed)
        var hasDevice = dev.StdOut
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Trim().EndsWith("\tdevice", StringComparison.OrdinalIgnoreCase));

        if (!hasDevice)
        {
            issues.Add(new HealthIssue
            {
                Code = "adb.no.device",
                Title = "No ADB device connected",
                Details = "No emulator/device is visible in 'adb devices -l'. Start emulator then recheck.",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.None
            });
        }

        return issues;
    }

    public async Task<int> FixAllAutoAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        string adbExe;
        try
        {
            adbExe = _provisioning.EnsureProvisioned();
        }
        catch
        {
            return 0;
        }

        // Real auto-fix: restart server
        _ = await RunProcessAsync(adbExe, "kill-server", ct);
        var start = await RunProcessAsync(adbExe, "start-server", ct);

        return start.ExitCode == 0 ? 1 : 0;
    }

    private static async Task<ProcResult> RunProcessAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };
        p.Start();

        // Avoid deadlocks: read streams in parallel
        var oTask = p.StandardOutput.ReadToEndAsync();
        var eTask = p.StandardError.ReadToEndAsync();

        await Task.WhenAll(p.WaitForExitAsync(ct), oTask, eTask);

        return new ProcResult(p.ExitCode, oTask.Result ?? string.Empty, eTask.Result ?? string.Empty);
    }

    private readonly record struct ProcResult(int ExitCode, string StdOut, string StdErr)
    {
        public string ErrorOrOutput => string.IsNullOrWhiteSpace(StdErr) ? StdOut : StdErr;
    }
}
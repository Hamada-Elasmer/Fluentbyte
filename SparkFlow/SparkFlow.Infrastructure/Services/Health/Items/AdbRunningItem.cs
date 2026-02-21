/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/AdbRunningItem.cs
 * Purpose: Core component: AdbRunningItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Diagnostics;
using AdbLib.Abstractions;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class AdbRunningItem : IHealthCheckItem
{
    private readonly IAdbProvisioning _provisioning;

    public HealthCheckItemId Id => HealthCheckItemId.AdbRunning;
    public string Title => "ADB Running";

    public AdbRunningItem(IAdbProvisioning provisioning)
    {
        _provisioning = provisioning ?? throw new ArgumentNullException(nameof(provisioning));
    }

    public async Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // 1) Ensure adb exists (auto-provision from bundled -> runtime)
        string adbExe;
        try
        {
            adbExe = _provisioning.EnsureProvisioned();
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.provision_failed",
                Title = "ADB provisioning failed",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto,
                ManualSteps = "Ensure bundled platform-tools exists and try again."
            };
        }

        // 2) Verify adb can run
        var ver = await RunProcessAsync(adbExe, "version", ct);
        if (ver.ExitCode != 0)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.cannot_run",
                Title = "ADB cannot run",
                Details = ver.ErrorOrOutput,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            };
        }

        // 3) Start server (safe)
        var start = await RunProcessAsync(adbExe, "start-server", ct);
        if (start.ExitCode != 0)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.server_failed",
                Title = "ADB server failed",
                Details = start.ErrorOrOutput,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            };
        }

        return null;
    }

    public async Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        string adbExe;
        try
        {
            adbExe = _provisioning.EnsureProvisioned();
        }
        catch
        {
            return false;
        }

        await RunProcessAsync(adbExe, "kill-server", ct);
        var start = await RunProcessAsync(adbExe, "start-server", ct);

        return start.ExitCode == 0;
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

        // Avoid deadlocks: run all awaits together
        var oTask = p.StandardOutput.ReadToEndAsync();
        var eTask = p.StandardError.ReadToEndAsync();
        await Task.WhenAll(p.WaitForExitAsync(ct), oTask, eTask);

        return new ProcResult(p.ExitCode, oTask.Result, eTask.Result);
    }

    private readonly record struct ProcResult(int ExitCode, string StdOut, string StdErr)
    {
        public string ErrorOrOutput => string.IsNullOrWhiteSpace(StdErr) ? StdOut : StdErr;
    }
}
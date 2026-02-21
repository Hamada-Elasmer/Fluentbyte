﻿/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/ResolutionOkItem.cs
 * Purpose: Health item: ResolutionOkItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Verifies emulator/device resolution is acceptable.
 * ============================================================================ */

 using System.Text.RegularExpressions;
 using AdbLib.Abstractions;
 using SparkFlow.Abstractions.Models;
 using SparkFlow.Abstractions.Services.Health.Abstractions;

 namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class ResolutionOkItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;

    public HealthCheckItemId Id => HealthCheckItemId.Adb_ResolutionOk;
    public string Title => "Resolution OK";

    private const int MinWidth = 960;
    private const int MinHeight = 540;

    public ResolutionOkItem(IAdbClient adb)
        => _adb = adb ?? throw new ArgumentNullException(nameof(adb));

    public async Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(ctx.AdbSerial))
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.serial_missing",
                Title = "No ADB serial",
                Details = "Profile has no AdbSerial yet. Binding must run first.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Manual,
                ManualSteps =
                    "1) Run HealthCheck FixAll.\n" +
                    "2) Ensure BindAdbSerialFromInstance succeeds.\n" +
                    "3) Recheck."
            };
        }

        try
        {
            await _adb.WaitForDeviceReadyAsync(ctx.AdbSerial, ct).ConfigureAwait(false);

            var txt = await _adb.ShellAsync(ctx.AdbSerial, "wm size", 15_000, ct).ConfigureAwait(false);

            // Example output:
            // Physical size: 1280x720
            var m = Regex.Match(txt ?? "", @"Physical\s+size:\s*(\d+)\s*x\s*(\d+)",
                RegexOptions.IgnoreCase);

            if (!m.Success)
            {
                return new HealthIssue
                {
                    Code = $"health.{Id}.parse_failed",
                    Title = "Failed to read resolution",
                    Details = txt,
                    Severity = HealthIssueSeverity.Warning,
                    FixKind = HealthFixKind.None
                };
            }

            var w = int.Parse(m.Groups[1].Value);
            var h = int.Parse(m.Groups[2].Value);

            if (w >= MinWidth && h >= MinHeight)
                return null;

            return new HealthIssue
            {
                Code = $"health.{Id}.too_low",
                Title = "Resolution too low",
                Details = $"Current: {w}x{h}. Recommended >= {MinWidth}x{MinHeight}.",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.Manual,
                ManualSteps =
                    "1) Open emulator settings.\n" +
                    "2) Increase resolution.\n" +
                    "3) Recommended: 1280x720 or higher.\n" +
                    "4) Re-run Health Check."
            };
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.failed",
                Title = "Resolution check failed",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.None
            };
        }
    }

    public Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
        => Task.FromResult(false);
}

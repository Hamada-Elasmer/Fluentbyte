﻿/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/ScreenshotWorksItem.cs
 * Purpose: Health item: ScreenshotWorksItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Verifies that adb screenshot works.
 * ============================================================================ */

 using AdbLib.Abstractions;
 using SparkFlow.Abstractions.Models;
 using SparkFlow.Abstractions.Services.Health.Abstractions;

 namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class ScreenshotWorksItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;

    public HealthCheckItemId Id => HealthCheckItemId.Adb_ScreenshotWorks;
    public string Title => "Screenshot Works";

    public ScreenshotWorksItem(IAdbClient adb)
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

            var bytes = await _adb.ScreenshotPngAsync(ctx.AdbSerial, 35_000, ct).ConfigureAwait(false);

            if (bytes is { Length: > 2000 })
                return null;

            return new HealthIssue
            {
                Code = $"health.{Id}.empty",
                Title = "Screenshot returned empty data",
                Details = $"Screenshot bytes length={bytes?.Length ?? 0}.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            };
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.failed",
                Title = "Screenshot failed",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            };
        }
    }

    public async Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(ctx.AdbSerial))
            return false;

        try
        {
            await _adb.KillServerAsync(ct: ct).ConfigureAwait(false);
            await _adb.StartServerAsync(ct: ct).ConfigureAwait(false);

            // Best-effort TCP reconnect (emulators)
            if (ctx.AdbSerial.Contains(':'))
            {
                try { await _adb.RunRawAsync($"connect {ctx.AdbSerial}", ct: ct).ConfigureAwait(false); }
                catch { /* ignore */ }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

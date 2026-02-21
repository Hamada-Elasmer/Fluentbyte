﻿/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/WarAndOrderLaunchItem.cs
 * Purpose: Health item: WarAndOrderLaunchItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Launches War and Order using StartActivity and verifies top activity.
 * ============================================================================ */

 using AdbLib.Abstractions;
 using GameModules.WarAndOrder.Common;
 using SparkFlow.Abstractions.Models;
 using SparkFlow.Abstractions.Services.Health.Abstractions;

 namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class WarAndOrderLaunchItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;

    public HealthCheckItemId Id => HealthCheckItemId.Game_WarAndOrderLaunch;
    public string Title => "War and Order Launch";

    public WarAndOrderLaunchItem(IAdbClient adb)
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

            await _adb.StartActivityAsync(ctx.AdbSerial, WarAndOrderConstants.MainActivity, 30_000, ct).ConfigureAwait(false);

            var top = await _adb.GetTopActivityAsync(ctx.AdbSerial, 25_000, ct).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(top) &&
                top.Contains(WarAndOrderConstants.PackageName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new HealthIssue
            {
                Code = $"health.{Id}.not_foreground",
                Title = "Game not in foreground",
                Details = $"TopActivity did not show game package. Output: {top}",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.Auto
            };
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.launch_failed",
                Title = "Game launch failed",
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
            await _adb.WaitForDeviceReadyAsync(ctx.AdbSerial, ct).ConfigureAwait(false);

            await _adb.ForceStopPackageAsync(ctx.AdbSerial, WarAndOrderConstants.PackageName, 20_000, ct).ConfigureAwait(false);

            await _adb.StartActivityAsync(ctx.AdbSerial, WarAndOrderConstants.MainActivity, 30_000, ct).ConfigureAwait(false);

            var top = await _adb.GetTopActivityAsync(ctx.AdbSerial, 25_000, ct).ConfigureAwait(false);

            return !string.IsNullOrWhiteSpace(top) &&
                   top.Contains(WarAndOrderConstants.PackageName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

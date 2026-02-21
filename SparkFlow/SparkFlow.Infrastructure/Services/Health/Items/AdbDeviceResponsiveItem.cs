﻿/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/AdbDeviceResponsiveItem.cs
 * Purpose: Health item: AdbDeviceResponsiveItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Ensures device responds to adb shell commands.
 * ============================================================================ */

 using AdbLib.Abstractions;
 using SparkFlow.Abstractions.Models;
 using SparkFlow.Abstractions.Services.Health.Abstractions;

 namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class AdbDeviceResponsiveItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;

    public HealthCheckItemId Id => HealthCheckItemId.Adb_DeviceResponsive;
    public string Title => "Device Responsive";

    public AdbDeviceResponsiveItem(IAdbClient adb)
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

            var txt = await _adb.ShellAsync(ctx.AdbSerial, "echo ok", 10_000, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(txt) || !txt.Contains("ok", StringComparison.OrdinalIgnoreCase))
            {
                return new HealthIssue
                {
                    Code = $"health.{Id}.shell_failed",
                    Title = "ADB shell not responsive",
                    Details = $"Shell returned unexpected output: '{txt}'",
                    Severity = HealthIssueSeverity.Blocker,
                    FixKind = HealthFixKind.Auto
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.not_ready",
                Title = "Device not responsive",
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

            // Verify by real shell
            await _adb.WaitForDeviceReadyAsync(ctx.AdbSerial, ct).ConfigureAwait(false);
            var txt = await _adb.ShellAsync(ctx.AdbSerial, "echo ok", 10_000, ct).ConfigureAwait(false);

            return !string.IsNullOrWhiteSpace(txt) &&
                   txt.Contains("ok", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

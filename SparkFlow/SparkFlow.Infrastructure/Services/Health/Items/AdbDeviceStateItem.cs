﻿/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/AdbDeviceStateItem.cs
 * Purpose: Health item: AdbDeviceStateItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Verifies that the device state is "device".
 * ============================================================================ */

 using AdbLib.Abstractions;
 using SparkFlow.Abstractions.Models;
 using SparkFlow.Abstractions.Services.Health.Abstractions;

 namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class AdbDeviceStateItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;

    public HealthCheckItemId Id => HealthCheckItemId.Adb_DeviceState;
    public string Title => "Device State";

    public AdbDeviceStateItem(IAdbClient adb)
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
            var devices = await _adb.DevicesAsync(ct: ct).ConfigureAwait(false);
            var d = devices.FirstOrDefault(x =>
                string.Equals(x.Serial, ctx.AdbSerial, StringComparison.OrdinalIgnoreCase));

            if (d is null)
            {
                return new HealthIssue
                {
                    Code = $"health.{Id}.not_listed",
                    Title = "Device not listed",
                    Details = $"Device '{ctx.AdbSerial}' is not present in adb devices.",
                    Severity = HealthIssueSeverity.Blocker,
                    FixKind = HealthFixKind.Auto
                };
            }

            if (string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase))
                return null;

            if (string.Equals(d.State, "unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                return new HealthIssue
                {
                    Code = $"health.{Id}.unauthorized",
                    Title = "Device unauthorized",
                    Details = $"Device '{ctx.AdbSerial}' is unauthorized.",
                    Severity = HealthIssueSeverity.Blocker,
                    FixKind = HealthFixKind.Manual,
                    ManualSteps =
                        "1) Open emulator/device screen.\n" +
                        "2) Accept the USB debugging authorization popup.\n" +
                        "3) Re-run Health Check."
                };
            }

            return new HealthIssue
            {
                Code = $"health.{Id}.state_{d.State}",
                Title = "Device not ready",
                Details = $"Device '{ctx.AdbSerial}' is in state '{d.State}'. Expected 'device'.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            };
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.exception",
                Title = "adb devices failed",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Auto
            };
        }
    }

    public async Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await _adb.KillServerAsync(ct: ct).ConfigureAwait(false);
            await _adb.StartServerAsync(ct: ct).ConfigureAwait(false);

            // Best-effort TCP reconnect (emulators)
            if (!string.IsNullOrWhiteSpace(ctx.AdbSerial) && ctx.AdbSerial.Contains(':'))
            {
                try { await _adb.RunRawAsync($"connect {ctx.AdbSerial}", ct: ct).ConfigureAwait(false); }
                catch { /* ignore */ }
            }

            // Verify
            var devices = await _adb.DevicesAsync(ct: ct).ConfigureAwait(false);
            var d = devices.FirstOrDefault(x =>
                string.Equals(x.Serial, ctx.AdbSerial, StringComparison.OrdinalIgnoreCase));

            return d is not null;
        }
        catch
        {
            return false;
        }
    }
}

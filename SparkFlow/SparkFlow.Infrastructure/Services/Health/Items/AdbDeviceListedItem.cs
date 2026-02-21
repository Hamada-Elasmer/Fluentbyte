﻿/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/AdbDeviceListedItem.cs
 * Purpose: Health item: AdbDeviceListedItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Checks if AdbSerial exists in "adb devices".
 * ============================================================================ */

 using AdbLib.Abstractions;
 using SparkFlow.Abstractions.Models;
 using SparkFlow.Abstractions.Services.Health.Abstractions;

 namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class AdbDeviceListedItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;
    private IHealthCheckItem _healthCheckItemImplementation;
    private IHealthCheckItem _healthCheckItemImplementation1;

    public HealthCheckItemId Id => HealthCheckItemId.Adb_DeviceListed;
    public string Title => "Device Listed in adb devices";

    public AdbDeviceListedItem(IAdbClient adb)
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
            var hit = devices.FirstOrDefault(x =>
                string.Equals(x.Serial, ctx.AdbSerial, StringComparison.OrdinalIgnoreCase));

            if (hit is not null)
                return null;

            return new HealthIssue
            {
                Code = $"health.{Id}.not_found",
                Title = "Device not listed",
                Details = $"Device '{ctx.AdbSerial}' was not found in adb devices list.",
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

            // best-effort verification
            _ = await _adb.DevicesAsync(ct: ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

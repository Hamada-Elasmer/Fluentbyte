/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/WarAndOrderInstalledItem.cs
 * Purpose: Health item: WarAndOrderInstalledItem.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Verifies War and Order package exists on device.
 * ============================================================================ */

using AdbLib.Abstractions;
using GameModules.WarAndOrder.Common;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class WarAndOrderInstalledItem : IHealthCheckItem
{
    private readonly IAdbClient _adb;

    public HealthCheckItemId Id => HealthCheckItemId.Game_WarAndOrderInstalled;
    public string Title => "War and Order Installed";

    public WarAndOrderInstalledItem(IAdbClient adb)
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

            var txt = await _adb.ShellAsync(
                ctx.AdbSerial,
                $"pm path {WarAndOrderConstants.PackageName}",
                20_000,
                ct).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(txt) &&
                txt.Contains("package:", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new HealthIssue
            {
                Code = $"health.{Id}.missing",
                Title = "War and Order not installed",
                Details = $"Package '{WarAndOrderConstants.PackageName}' not found.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Manual,
                ManualSteps =
                    "1) Open Google Play inside the emulator.\n" +
                    "2) Install War and Order.\n" +
                    $"3) Link: {WarAndOrderConstants.PlayStoreUrl}\n" +
                    "4) Re-run Health Check."
            };
        }
        catch (Exception ex)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.exception",
                Title = "Failed to check game installation",
                Details = ex.Message,
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Manual,
                ManualSteps =
                    "1) Ensure emulator/device is running.\n" +
                    "2) Ensure adb devices shows the device as 'device'.\n" +
                    "3) Re-run Health Check."
            };
        }
    }

    public Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
        => Task.FromResult(false);
}

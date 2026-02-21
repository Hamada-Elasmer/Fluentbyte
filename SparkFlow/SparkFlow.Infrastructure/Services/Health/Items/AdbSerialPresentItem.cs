/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Items/AdbSerialPresentItem.cs
 * Purpose: Health item: AdbSerialPresentItem.
 * Notes:
 *  - This item only checks if AdbSerial is present.
 *  - Auto-binding is handled by BindAdbSerialFromInstanceItem.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Items;

public sealed class AdbSerialPresentItem : IHealthCheckItem
{
    private readonly IProfilesStore _profiles;

    public HealthCheckItemId Id => HealthCheckItemId.Adb_SerialPresent;
    public string Title => "ADB Serial Bound";

    public AdbSerialPresentItem(IProfilesStore profiles)
        => _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));

    public async Task<HealthIssue?> CheckAsync(HealthCheckContext ctx, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var p = await _profiles.GetByIdAsync(ctx.ProfileId, ct).ConfigureAwait(false);

        if (p is null)
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.profile_missing",
                Title = "Profile missing",
                Details = $"Profile '{ctx.ProfileId}' was not found.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.None
            };
        }

        if (string.IsNullOrWhiteSpace(p.AdbSerial))
        {
            return new HealthIssue
            {
                Code = $"health.{Id}.missing",
                Title = "ADB Serial not bound",
                Details = "Profile has no AdbSerial. Run Bind step to auto-bind.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Manual,
                ManualSteps =
                    "1) Run HealthCheck FixAll.\n" +
                    "2) Ensure InstanceId exists and emulator can start.\n" +
                    "3) Recheck."
            };
        }

        return null;
    }

    public Task<bool> TryAutoFixAsync(HealthCheckContext ctx, CancellationToken ct = default)
        => Task.FromResult(false);
}

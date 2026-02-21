/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Accounts/ProfileValidationService.cs
 * Purpose: Profile validation (Start/Stop instance check) - non-blocking.
 * Notes:
 *  - Runs in background worker (sequential).
 *  - Does NOT use ADB or serial binding.
 * ============================================================================ */

using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Engine.Services;

public sealed class ProfileValidationService(
    IProfilesStore profilesStore,
    IEmulatorInstanceControlService emulator)
{
    private readonly IProfilesStore _profilesStore =
        profilesStore ?? throw new ArgumentNullException(nameof(profilesStore));

    private readonly IEmulatorInstanceControlService _emulator =
        emulator ?? throw new ArgumentNullException(nameof(emulator));

    public bool IsReadyForValidation(AccountProfile profile)
        => !string.IsNullOrWhiteSpace(profile.InstanceId);

    /// <summary>
    /// Validates a single profile by starting and stopping its emulator instance.
    /// This method never throws to callers; it writes status back to the profile.
    /// </summary>
    public async Task ValidateAsync(AccountProfile profile, CancellationToken ct = default)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        ct.ThrowIfCancellationRequested();

        // If incomplete, keep it as Draft (and clear pending state).
        if (!IsReadyForValidation(profile))
        {
            profile.ValidationStatus = ProfileValidationStatus.Draft;
            profile.ValidationError = null;
            profile.ValidatedAt = null;
            await _profilesStore.SaveAsync(profile, ct).ConfigureAwait(false);
            return;
        }

        // Only validate if pending (or previously failed). Never re-validate Validated.
        if (profile.ValidationStatus == ProfileValidationStatus.Validated)
            return;

        try
        {
            await _emulator.RefreshAsync(ct).ConfigureAwait(false);

            await _emulator.StartAsync(
                profile.InstanceId!,
                waitUntilRunning: true,
                timeoutMs: 90_000,
                ct: ct).ConfigureAwait(false);

            await Task.Delay(2500, ct).ConfigureAwait(false);

            await _emulator.StopAsync(
                profile.InstanceId!,
                waitUntilStopped: true,
                timeoutMs: 60_000,
                ct: ct).ConfigureAwait(false);

            profile.ValidationStatus = ProfileValidationStatus.Validated;
            profile.ValidationError = null;
            profile.ValidatedAt = DateTimeOffset.UtcNow;

            await _profilesStore.SaveAsync(profile, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            profile.ValidationStatus = ProfileValidationStatus.ValidationFailed;
            profile.ValidationError = SimplifyError(ex);
            profile.ValidatedAt = null;

            await _profilesStore.SaveAsync(profile, ct).ConfigureAwait(false);
        }
    }

    private static string SimplifyError(Exception ex)
    {
        var msg = ex.Message?.Trim();
        if (string.IsNullOrWhiteSpace(msg))
            msg = ex.GetType().Name;

        const int maxLen = 400;
        return msg.Length <= maxLen ? msg : msg[..maxLen];
    }
}

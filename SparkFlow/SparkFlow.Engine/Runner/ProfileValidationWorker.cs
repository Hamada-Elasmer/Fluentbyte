/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Runner/ProfileValidationWorker.cs
 * Purpose: Background loop to validate newly added profiles (Start/Stop check).
 * Notes:
 *  - Sequential (one profile at a time).
 *  - Non-blocking to Add Profile flow.
 *  - Does NOT use ADB or serial mapping.
 * ============================================================================ */

using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Domain.Models.Accounts;
using SparkFlow.Engine.Services;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Engine.Runner;

public sealed class ProfileValidationWorker(
    IProfilesStore profiles,
    ProfileValidationService validator,
    MLogger logger)
{
    private readonly IProfilesStore _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    private readonly ProfileValidationService _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly MLogger _log = logger ?? MLogger.Instance;

    private const int IdleDelayMs = 10_000;
    private const int AfterWorkDelayMs = 1_500;

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var next = await GetNextPendingAsync(ct).ConfigureAwait(false);

                if (next is null)
                {
                    await Task.Delay(IdleDelayMs, ct).ConfigureAwait(false);
                    continue;
                }

                _log.Log(
                    LogComponent.Runner,
                    LogChannel.SYSTEM,
                    LogLevel.INFO,
                    $"[ProfileValidation] Validating ProfileId='{next.Id}', InstanceId='{next.InstanceId}'",
                    0,
                    "validation",
                    next.Id);

                await _validator.ValidateAsync(next, ct).ConfigureAwait(false);

                await Task.Delay(AfterWorkDelayMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.Exception(
                    LogComponent.Runner,
                    LogChannel.SYSTEM,
                    ex,
                    "[ProfileValidation] Worker loop faulted",
                    0,
                    "validation",
                    null);

                try
                {
                    await Task.Delay(IdleDelayMs, ct).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    private async Task<AccountProfile?> GetNextPendingAsync(CancellationToken ct)
    {
        var all = await _profiles.LoadAllAsync(ct).ConfigureAwait(false);

        return all
            .Where(p => p.ValidationStatus == ProfileValidationStatus.PendingValidation)
            .OrderBy(p => p.CreatedAt ?? DateTimeOffset.MaxValue)
            .ThenBy(p => p.UpdatedAt ?? DateTimeOffset.MaxValue)
            .FirstOrDefault();
    }
}

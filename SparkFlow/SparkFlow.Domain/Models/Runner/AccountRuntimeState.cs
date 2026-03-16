namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Per-profile runtime state used by the scheduler and policy engine.
/// This is a domain model (serialization-friendly, no locks).
/// Atomicity is handled by the store (IAccountRuntimeStore.Upsert).
/// </summary>
public sealed class AccountRuntimeState
{
    public string ProfileId { get; init; } = "";

    // =========================
    // Scheduling
    // =========================

    /// <summary>
    /// Next time this profile becomes eligible to run (UTC).
    /// </summary>
    public DateTimeOffset NextRunAtUtc { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Higher value means higher priority when multiple profiles are eligible.
    /// Keep small range (e.g., -10..+10) unless you have a stronger policy.
    /// </summary>
    public int Priority { get; set; } = 0;

    // =========================
    // Failures / Health
    // =========================

    public int ConsecutiveFailures { get; set; } = 0;

    /// <summary>
    /// If set and in the future, the profile is temporarily disabled (UTC).
    /// </summary>
    public DateTimeOffset? DisabledUntilUtc { get; set; }

    // =========================
    // Observability
    // =========================

    public DateTimeOffset? LastStartedUtc { get; set; }
    public DateTimeOffset? LastFinishedUtc { get; set; }

    public bool? LastSuccess { get; set; }

    public RunnerFailureType? LastFailureType { get; set; }
    public string? LastFailureMessage { get; set; }
    public string? LastFailureCode { get; set; }

    /// <summary>
    /// Useful for UI to know the state is “fresh”.
    /// Store implementations may update this on any change.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsDisabled(DateTimeOffset nowUtc)
        => DisabledUntilUtc.HasValue && DisabledUntilUtc.Value > nowUtc;

    /// <summary>
    /// Extends (never shortens) the disabled window.
    /// </summary>
    public void DisableFor(TimeSpan duration, DateTimeOffset nowUtc, RunnerFailure failure)
    {
        if (duration <= TimeSpan.Zero) return;

        var until = nowUtc.Add(duration);

        if (!DisabledUntilUtc.HasValue || until > DisabledUntilUtc.Value)
            DisabledUntilUtc = until;

        LastSuccess = false;
        LastFailureType = failure.Type;
        LastFailureMessage = failure.Message;
        LastFailureCode = failure.Code;

        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSuccess(DateTimeOffset nowUtc)
    {
        ConsecutiveFailures = 0;
        LastSuccess = true;
        LastFailureType = null;
        LastFailureMessage = null;
        LastFailureCode = null;

        LastFinishedUtc ??= nowUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailure(DateTimeOffset nowUtc, RunnerFailure failure)
    {
        ConsecutiveFailures++;
        LastSuccess = false;
        LastFailureType = failure.Type;
        LastFailureMessage = failure.Message;
        LastFailureCode = failure.Code;

        LastFinishedUtc ??= nowUtc;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
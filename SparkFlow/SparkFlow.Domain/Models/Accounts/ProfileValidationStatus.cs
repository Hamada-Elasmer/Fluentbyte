namespace SparkFlow.Domain.Models.Accounts;

public enum ProfileValidationStatus
{
    /// <summary>
    /// Default / incomplete profile data, not ready for validation.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Saved and queued for validation (Start/Stop check).
    /// </summary>
    PendingValidation = 1,

    /// <summary>
    /// Validation succeeded.
    /// </summary>
    Validated = 2,

    /// <summary>
    /// Validation failed (but profile remains saved).
    /// </summary>
    ValidationFailed = 3,
}
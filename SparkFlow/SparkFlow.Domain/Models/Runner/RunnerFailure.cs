namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Stable failure categories used by scheduling/backoff/circuit-breakers and metrics.
/// IMPORTANT: keep numeric values stable (do not reorder).
/// </summary>
public enum RunnerFailureType
{
    None = 0,

    // Input / configuration
    Skipped_NoSerial = 10,

    // Emulator
    EmulatorStartFailed = 20,

    // Device / ADB
    DeviceReadyTimeout = 30,
    DeviceUnreachable = 31,
    AdbFailure = 32,

    // Game
    GameNotInstalled = 40,
    GameLaunchFailed = 41,

    // Fallback
    Unknown = 1000
}

/// <summary>
/// A classified failure (domain-level) that can be logged, stored, and used for scheduling decisions.
/// </summary>
public sealed record RunnerFailure(
    RunnerFailureType Type,
    string Message,
    bool IsTransient = true,
    string? Code = null)
{
    public static RunnerFailure None() => new(RunnerFailureType.None, string.Empty, IsTransient: true);

    public override string ToString()
        => string.IsNullOrWhiteSpace(Code)
            ? $"{Type}: {Message}"
            : $"{Type} ({Code}): {Message}";
}
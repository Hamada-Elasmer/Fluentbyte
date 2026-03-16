namespace SparkFlow.Domain.Models.Runner;

public sealed record PolicyOutcome(
    bool Success,
    RunnerFailure? Failure,
    int Attempts,
    TimeSpan TotalDelay,
    PolicyDecision Decision);
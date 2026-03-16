namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Decision returned by policy engine.
/// Runner must apply this atomically.
/// </summary>
public sealed record PolicyDecision(
    DateTimeOffset NextRunAtUtc,
    TimeSpan? DisableFor,
    bool OpenCircuit,
    CircuitScope Scope);
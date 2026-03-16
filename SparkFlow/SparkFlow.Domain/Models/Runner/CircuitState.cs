namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Enterprise circuit breaker state machine.
/// </summary>
public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}
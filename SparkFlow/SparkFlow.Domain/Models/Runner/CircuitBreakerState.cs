using System.Collections.Generic;

namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Enterprise-ready circuit breaker state machine.
/// Thread-safe.
/// Supports Closed/Open/HalfOpen with progressive penalty.
/// </summary>
public sealed class CircuitBreakerState
{
    private readonly object _gate = new();

    public string Key { get; init; } = string.Empty;

    public CircuitState State { get; private set; } = CircuitState.Closed;

    public int ConsecutiveFailures { get; private set; }

    public DateTimeOffset? OpenUntilUtc { get; private set; }

    public DateTimeOffset? LastOpenedAtUtc { get; private set; }

    private bool _probeInProgress;

    /// <summary>
    /// Returns true if execution should be blocked.
    /// Handles transition from Open → HalfOpen.
    /// </summary>
    public bool ShouldBlock(DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            if (State == CircuitState.Closed)
                return false;

            if (State == CircuitState.Open)
            {
                if (OpenUntilUtc.HasValue && OpenUntilUtc.Value <= nowUtc)
                {
                    State = CircuitState.HalfOpen;
                    _probeInProgress = false;
                    return false;
                }

                return true;
            }

            if (State == CircuitState.HalfOpen)
            {
                if (_probeInProgress)
                    return true;

                _probeInProgress = true;
                return false;
            }

            return false;
        }
    }

    public void MarkFailure(DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            ConsecutiveFailures++;
        }
    }

    public void MarkSuccess()
    {
        lock (_gate)
        {
            Reset();
        }
    }

    public void Open(DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            var duration = ComputeOpenDuration();
            OpenUntilUtc = nowUtc.Add(duration);
            LastOpenedAtUtc = nowUtc;
            State = CircuitState.Open;
            _probeInProgress = false;
        }
    }

    private TimeSpan ComputeOpenDuration()
    {
        // Progressive penalty: 30s → 60s → 120s → 300s cap
        var baseSeconds = 30 * Math.Pow(2, Math.Min(3, ConsecutiveFailures - 1));
        return TimeSpan.FromSeconds(Math.Min(300, baseSeconds));
    }

    public void Reset()
    {
        ConsecutiveFailures = 0;
        OpenUntilUtc = null;
        State = CircuitState.Closed;
        _probeInProgress = false;
    }
}
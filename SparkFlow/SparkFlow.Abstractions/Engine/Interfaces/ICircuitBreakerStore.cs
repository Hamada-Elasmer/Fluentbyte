using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Abstractions.Engine.Interfaces;

public interface ICircuitBreakerStore
{
    CircuitBreakerState GetOrCreate(string key);

    bool TryGet(string key, out CircuitBreakerState state);

    bool Remove(string key);

    IReadOnlyList<CircuitBreakerState> Snapshot();

    void Clear();
}
using System.Collections.Concurrent;
using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Runner;

public sealed class InMemoryCircuitBreakerStore : ICircuitBreakerStore
{
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _map = new();

    public CircuitBreakerState GetOrCreate(string key)
        => _map.GetOrAdd(key, k => new CircuitBreakerState { Key = k });

    public bool TryGet(string key, out CircuitBreakerState state)
        => _map.TryGetValue(key, out state!);

    public bool Remove(string key)
        => _map.TryRemove(key, out _);

    public IReadOnlyList<CircuitBreakerState> Snapshot()
        => _map.Values.ToList();

    public void Clear()
        => _map.Clear();
}
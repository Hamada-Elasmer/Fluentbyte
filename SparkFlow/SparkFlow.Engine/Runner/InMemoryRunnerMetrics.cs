using System.Collections.Concurrent;
using SparkFlow.Abstractions.Engine.Interfaces;

namespace SparkFlow.Engine.Runner;

public sealed class InMemoryRunnerMetrics : IRunnerMetrics
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, long> _timersMs = new();

    public void Inc(string name, long value = 1)
        => _counters.AddOrUpdate(name, value, (_, old) => old + value);

    public void ObserveMs(string name, long ms)
        => _timersMs.AddOrUpdate(name, ms, (_, old) => Math.Max(old, ms));

    public IReadOnlyDictionary<string, long> SnapshotCounters() => _counters;
    public IReadOnlyDictionary<string, long> SnapshotTimersMs() => _timersMs;
}
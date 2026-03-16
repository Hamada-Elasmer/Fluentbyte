namespace SparkFlow.Abstractions.Engine.Interfaces;

public interface IRunnerMetrics
{
    void Inc(string name, long value = 1);
    void ObserveMs(string name, long ms);
    IReadOnlyDictionary<string, long> SnapshotCounters();
    IReadOnlyDictionary<string, long> SnapshotTimersMs();
}
using System.Collections.Concurrent;
using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Engine.Scheduler;

/// <summary>
/// Enterprise-grade in-memory runtime store.
/// - Thread-safe
/// - Copy-on-write
/// - Atomic Upsert
/// - Safe snapshots
/// </summary>
public sealed class InMemoryAccountRuntimeStore : IAccountRuntimeStore
{
    private readonly ConcurrentDictionary<string, AccountRuntimeState> _map = new();

    /// <summary>
    /// Returns state if exists. Does NOT create implicitly.
    /// </summary>
    public bool TryGet(string profileId, out AccountRuntimeState state)
        => _map.TryGetValue(profileId, out state!);

    /// <summary>
    /// Creates state only if missing.
    /// </summary>
    public AccountRuntimeState GetOrCreate(string profileId)
        => _map.GetOrAdd(profileId, id => new AccountRuntimeState { ProfileId = id });

    /// <summary>
    /// Atomic update using copy-on-write strategy.
    /// The update function MUST return a NEW instance.
    /// </summary>
    public AccountRuntimeState Upsert(
        string profileId,
        Func<AccountRuntimeState, AccountRuntimeState> update)
    {
        while (true)
        {
            var current = _map.GetOrAdd(profileId,
                id => new AccountRuntimeState { ProfileId = id });

            var updated = update(Clone(current));

            if (_map.TryUpdate(profileId, updated, current))
                return updated;

            // Retry if concurrent modification occurred
        }
    }

    public bool Remove(string profileId)
        => _map.TryRemove(profileId, out _);

    public IReadOnlyList<AccountRuntimeState> Snapshot()
        => _map.Values.Select(Clone).ToList();

    public void Clear()
        => _map.Clear();

    private static AccountRuntimeState Clone(AccountRuntimeState s)
        => new()
        {
            ProfileId = s.ProfileId,
            NextRunAtUtc = s.NextRunAtUtc,
            Priority = s.Priority,
            ConsecutiveFailures = s.ConsecutiveFailures,
            DisabledUntilUtc = s.DisabledUntilUtc,
            LastStartedUtc = s.LastStartedUtc,
            LastFinishedUtc = s.LastFinishedUtc,
            LastSuccess = s.LastSuccess,
            LastFailureType = s.LastFailureType,
            LastFailureMessage = s.LastFailureMessage,
            LastFailureCode = s.LastFailureCode,
            UpdatedAtUtc = s.UpdatedAtUtc
        };
}
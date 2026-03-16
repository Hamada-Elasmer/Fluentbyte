using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Engine.Engine.Scheduler;

public sealed class NextRunScheduler : IAccountScheduler
{
    private readonly IAccountRuntimeStore _store;
    private readonly ICircuitBreakerStore _breaker;

    public NextRunScheduler(
        IAccountRuntimeStore store,
        ICircuitBreakerStore breaker)
    {
        _store = store;
        _breaker = breaker;
    }

    public IReadOnlyList<AccountProfile> SelectNextBatch(
        IReadOnlyList<AccountProfile> enabledAccounts,
        DateTimeOffset nowUtc,
        int maxBatchSize)
    {
        var result = new List<AccountProfile>();

        foreach (var acc in enabledAccounts)
        {
            var id = acc.Id?.ToString();
            if (string.IsNullOrWhiteSpace(id)) continue;

            if (!_store.TryGet(id, out var st))
                continue;

            if (st.IsDisabled(nowUtc))
                continue;

            if (st.NextRunAtUtc > nowUtc)
                continue;

            var circuitKey = $"acc:{id}";
            if (_breaker.TryGet(circuitKey, out var breakerState))
            {
                if (breakerState.ShouldBlock(nowUtc))
                    continue;
            }

            result.Add(acc);
        }

        return result
            .OrderByDescending(a =>
            {
                _store.TryGet(a.Id!.ToString(), out var s);
                return s?.Priority ?? 0;
            })
            .Take(maxBatchSize)
            .ToList();
    }
}
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Abstractions.Engine.Interfaces;

public interface IAccountRuntimeStore
{
    AccountRuntimeState GetOrCreate(string profileId);

    bool TryGet(string profileId, out AccountRuntimeState state);

    /// <summary>
    /// Atomic update for a specific profile state.
    /// Store implementation decides locking strategy.
    /// </summary>
    AccountRuntimeState Upsert(string profileId, Func<AccountRuntimeState, AccountRuntimeState> update);

    bool Remove(string profileId);

    IReadOnlyList<AccountRuntimeState> Snapshot();

    void Clear();
}
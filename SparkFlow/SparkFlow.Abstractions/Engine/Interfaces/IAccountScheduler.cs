using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Abstractions.Engine.Interfaces;

public interface IAccountScheduler
{
    IReadOnlyList<AccountProfile> SelectNextBatch(
        IReadOnlyList<AccountProfile> enabledAccounts,
        DateTimeOffset nowUtc,
        int maxBatchSize);
}
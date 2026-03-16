using SparkFlow.Domain.Models.Accounts;
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Full context provided to policy engine.
/// </summary>
public sealed record PolicyContext(
    AccountProfile Profile,
    AccountRuntimeState Runtime,
    DateTimeOffset NowUtc);
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Abstractions.Engine.Interfaces;

public interface IExecutionPolicyEngine
{
    Task<PolicyOutcome> ExecuteAsync(
        PolicyContext context,
        Func<CancellationToken, Task> runOnceAsync,
        CancellationToken ct);
}
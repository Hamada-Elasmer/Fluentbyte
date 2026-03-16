using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Runner;

public sealed class ExecutionPolicyEngine : IExecutionPolicyEngine
{
    private readonly IFailureClassifier _classifier;
    private readonly ICircuitBreakerStore _store;
    private readonly IRunnerMetrics _metrics;

    public ExecutionPolicyEngine(
        IFailureClassifier classifier,
        ICircuitBreakerStore store,
        IRunnerMetrics metrics)
    {
        _classifier = classifier;
        _store = store;
        _metrics = metrics;
    }

    public async Task<PolicyOutcome> ExecuteAsync(
        PolicyContext context,
        Func<CancellationToken, Task> runOnceAsync,
        CancellationToken ct)
    {
        var now = context.NowUtc;

        var accountKey = $"acc:{context.Profile.Id}";
        var breaker = _store.GetOrCreate(accountKey);

        if (breaker.ShouldBlock(now))
        {
            _metrics.Inc("policy.circuit.blocked");
            return new PolicyOutcome(
                false,
                new RunnerFailure(RunnerFailureType.AdbFailure, "Circuit open"),
                0,
                TimeSpan.Zero,
                new PolicyDecision(
                    context.Runtime.NextRunAtUtc,
                    null,
                    false,
                    CircuitScope.Account));
        }

        int attempts = 0;
        TimeSpan totalDelay = TimeSpan.Zero;

        try
        {
            attempts++;
            await runOnceAsync(ct);

            breaker.MarkSuccess();

            var next = now.AddMinutes(12);

            return new PolicyOutcome(
                true,
                null,
                attempts,
                totalDelay,
                new PolicyDecision(next, null, false, CircuitScope.Account));
        }
        catch (Exception ex)
        {
            var failure = ex is RunnerClassifiedException rc
                ? rc.Failure
                : _classifier.Classify(ex);

            breaker.MarkFailure(now);

            if (!failure.IsTransient)
            {
                breaker.Open(now);
                _metrics.Inc("policy.circuit.open");
            }

            var next = ComputeBackoff(now, context.Runtime.ConsecutiveFailures);

            return new PolicyOutcome(
                false,
                failure,
                attempts,
                totalDelay,
                new PolicyDecision(
                    next,
                    failure.IsTransient ? null : TimeSpan.FromMinutes(15),
                    !failure.IsTransient,
                    CircuitScope.Account));
        }
    }

    private static DateTimeOffset ComputeBackoff(DateTimeOffset now, int failures)
    {
        var minutes = Math.Min(20, 2 * Math.Max(1, failures));
        return now.AddMinutes(minutes);
    }
}
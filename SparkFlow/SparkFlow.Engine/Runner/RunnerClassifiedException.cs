using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Runner;

public sealed class RunnerClassifiedException : Exception
{
    public RunnerFailure Failure { get; }

    public string? Phase { get; }

    public RunnerClassifiedException(
        RunnerFailure failure,
        string? phase = null,
        Exception? inner = null)
        : base(failure.ToString(), inner)
    {
        Failure = failure ?? throw new ArgumentNullException(nameof(failure));
        Phase = phase;
    }
}
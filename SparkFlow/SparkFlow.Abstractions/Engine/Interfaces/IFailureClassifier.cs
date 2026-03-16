using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Abstractions.Engine.Interfaces;

public interface IFailureClassifier
{
    /// <summary>
    /// Maps an exception to a stable failure category (must be deterministic and never throw).
    /// </summary>
    RunnerFailure Classify(Exception ex);
}
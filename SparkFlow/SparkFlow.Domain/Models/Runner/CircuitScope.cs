namespace SparkFlow.Domain.Models.Runner;

/// <summary>
/// Defines logical circuit scopes.
/// Multiple circuits may exist simultaneously.
/// </summary>
public enum CircuitScope
{
    Account,
    Instance,
    Layer
}
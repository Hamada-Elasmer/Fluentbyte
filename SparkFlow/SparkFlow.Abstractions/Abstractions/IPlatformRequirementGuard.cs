/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.Abstractions/Abstractions/IPlatformRequirementGuard.cs
 * Purpose: Core abstraction: platform requirement guard for UI (Core-owned).
 * ============================================================================ */

namespace SparkFlow.Abstractions.Abstractions;

/// <summary>
/// Core-owned requirement guard exposed to UI.
/// UI must NOT depend on EmulatorLib concrete implementations.
/// </summary>
public interface IPlatformRequirementGuard
{
    RequirementResult Validate();
}

public sealed record RequirementResult(
    bool IsOk,
    string Title,
    string Message,
    IReadOnlyList<string> FixSteps
)
{
    public static RequirementResult Ok(
        string title = "OK",
        string message = "All requirements are satisfied.")
        => new(true, title, message, Array.Empty<string>());

    public static RequirementResult Blocked(
        string title,
        string message,
        params string[] steps)
        => new(false, title, message, steps);
}

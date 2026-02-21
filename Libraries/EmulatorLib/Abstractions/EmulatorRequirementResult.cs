namespace EmulatorLib.Abstractions;

public sealed record EmulatorRequirementResult(
    bool IsOk,
    string Title,
    string Message,
    IReadOnlyList<string> FixSteps
)
{
    public static EmulatorRequirementResult Ok(
        string title = "OK",
        string message = "All emulator requirements are satisfied.")
        => new(true, title, message, Array.Empty<string>());

    public static EmulatorRequirementResult Blocked(
        string title,
        string message,
        params string[] steps)
        => new(false, title, message, steps);
}

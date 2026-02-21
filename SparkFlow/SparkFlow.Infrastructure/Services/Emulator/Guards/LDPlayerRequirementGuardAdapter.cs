/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.Core/Services/Emulator/Guards/LDPlayerRequirementGuardAdapter.cs
 * Purpose: Core adapter: maps EmulatorLib requirement guard into Core abstraction.
 * ============================================================================ */

using EmulatorLib.Abstractions;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Emulator.Guards;

public sealed class LDPlayerRequirementGuardAdapter : IPlatformRequirementGuard
{
    private readonly IEmulatorRequirementGuard _guard;

    public LDPlayerRequirementGuardAdapter(IEmulatorRequirementGuard guard)
    {
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
    }

    public RequirementResult Validate()
    {
        var r = _guard.Validate();

        if (r.IsOk)
            return RequirementResult.Ok();

        var steps = (r.FixSteps ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        return RequirementResult.Blocked(
            string.IsNullOrWhiteSpace(r.Title) ? "Requirement Failed" : r.Title,
            string.IsNullOrWhiteSpace(r.Message) ? "Requirement check failed." : r.Message,
            steps);
    }
}

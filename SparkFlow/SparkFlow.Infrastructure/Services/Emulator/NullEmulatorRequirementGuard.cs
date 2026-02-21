/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.UI/Services/Emulator/NullEmulatorRequirementGuard.cs
 * Purpose: UI fallback: NullEmulatorRequirementGuard.
 * Notes:
 *  - Guarantees DI never fails.
 *  - Allows UI to load even when EmulatorLib is not ready.
 * ============================================================================ */

using EmulatorLib.Abstractions;

namespace SparkFlow.Infrastructure.Services.Emulator;

public sealed class NullEmulatorRequirementGuard : IEmulatorRequirementGuard
{
    public EmulatorRequirementResult Validate()
        => EmulatorRequirementResult.Ok();
}

/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Ports/Fakes/FakeEmulatorController.cs
 * Purpose: Library component: FakeEmulatorController.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameModules.WarAndOrder.Ports.Fakes;

/// <summary>
/// Minimal fake for emulator controller. Useful for tests.
/// </summary>
public sealed class FakeEmulatorController : IEmulatorController
{
    public Task<bool> EnsureInstanceRunningAsync(string emulatorInstanceId, CancellationToken ct)
        => Task.FromResult(true);

    public Task<string> ResolveDeviceIdAsync(string emulatorInstanceId, CancellationToken ct)
        => Task.FromResult("fake-device-1");
}
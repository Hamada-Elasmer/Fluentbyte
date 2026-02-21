/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameModules/WarAndOrder/Ports/IEmulatorController.cs
 * Purpose: Library component: IEmulatorController.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameModules.WarAndOrder.Ports;

/// <summary>
/// Optional port if you want the module to request emulator-level actions.
/// If you already handle emulator lifecycle in Core, you can ignore this.
/// </summary>
public interface IEmulatorController
{
    Task<bool> EnsureInstanceRunningAsync(string emulatorInstanceId, CancellationToken ct);
    Task<string> ResolveDeviceIdAsync(string emulatorInstanceId, CancellationToken ct);
}
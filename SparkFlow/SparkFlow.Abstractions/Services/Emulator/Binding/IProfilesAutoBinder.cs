/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Emulator/Binding/IProfilesAutoBinder.cs
 * Purpose: Core component: IProfilesAutoBinder.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Services.Emulator.Binding;

/// <summary>
/// Automatically binds unbound profiles (adbSerial == null) to free ready ADB devices.
///
/// This is important for a "single button" experience:
/// - Users can add accounts even when the emulator is not started yet.
/// - Once devices appear, SparkFlow binds them automatically in the background.
/// </summary>
public interface IProfilesAutoBinder
{
    /// <summary>
    /// Best-effort binding. Should never throw.
    /// </summary>
    Task AutoBindUnboundProfilesAsync(CancellationToken ct = default);
}
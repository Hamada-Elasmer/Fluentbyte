/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Emulator/AutoStart/IEmulatorAutoStarter.cs
 * Purpose: Core component: IEmulatorAutoStarter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Services.Emulator.AutoStart;

/// <summary>
/// Starts an emulator (best-effort) when SparkFlow needs a device but none are visible yet.
///
/// Design goals:
/// - "One click" UX: user presses Start and SparkFlow will start the emulator in the background.
/// - Must NEVER crash the UI. Failures are handled as warnings and the runner can still
///   proceed (or safely skip) based on ADB readiness.
/// - Must be platform-safe: a Null implementation is always registered.
/// </summary>
public interface IEmulatorAutoStarter
{
    /// <summary>
    /// Ensure at least one ADB device becomes ready.
    /// Implementation may choose to start an emulator instance and wait until it appears.
    /// This call should be best-effort: it may return even if no device became ready.
    /// </summary>
    Task EnsureAnyDeviceReadyAsync(CancellationToken ct = default);
}
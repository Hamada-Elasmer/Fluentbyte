/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/IRotationManager.cs
 * Purpose: Core component: IRotationManager.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Controls sequential rotation over enabled accounts.
/// Provides pause points that can be awaited frequently to respect Pause/Stop immediately.
/// </summary>
public interface IRotationManager
{
    /// <summary>Current account being processed (null if none).</summary>
    IAccountState? Current { get; }

    /// <summary>True when paused.</summary>
    bool IsPaused { get; }

    /// <summary>Pause rotation (takes effect at next PausePointAsync).</summary>
    void Pause();

    /// <summary>Resume rotation.</summary>
    void Resume();

    /// <summary>
    /// Runs exactly one full pass over the current queue (until Next() returns null).
    /// Caller is responsible for preparing the queue (Rebuild/Reset).
    /// </summary>
    Task RunOnceAsync(
        Func<IAccountState, CancellationToken, Task> runAccountAsync,
        CancellationToken ct);

    /// <summary>
    /// Common pause+stop boundary. Call this often (before/after long operations).
    /// </summary>
    Task PausePointAsync(CancellationToken ct);

    // Optional events (useful for logs/UI)
    event Action<IAccountState>? AccountStarted;
    event Action<IAccountState>? AccountFinished;
    event Action<IAccountState, Exception>? AccountFailed;
    event Action? RotationCompleted;
}
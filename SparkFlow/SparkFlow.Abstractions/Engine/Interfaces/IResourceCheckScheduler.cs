/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/IResourceCheckScheduler.cs
 * Purpose: Core component: IResourceCheckScheduler.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Repeatedly runs an async condition check until it becomes true,
/// waiting between attempts according to IWaitLogic.
/// </summary>
public interface IResourceCheckScheduler
{
    /// <summary>
    /// Fired when a check attempt starts (attemptNumber starts at 1).
    /// </summary>
    event Action<int>? CheckAttemptStarted;

    /// <summary>
    /// Fired when the check succeeds.
    /// </summary>
    event Action<int>? CheckSucceeded;

    /// <summary>
    /// Fired when the check returns false (condition not met).
    /// </summary>
    event Action<int>? CheckFailed;

    /// <summary>
    /// Fired when an exception happens during check (treated as a failed attempt).
    /// </summary>
    event Action<int, Exception>? CheckErrored;

    /// <summary>
    /// Wait until the condition becomes true. Never returns false unless canceled/exceptioned.
    /// </summary>
    Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> checkAsync,
        CancellationToken ct);

    /// <summary>
    /// Same as WaitUntilAsync but stops after timeout and returns false on timeout.
    /// Returns true if succeeded before timeout.
    /// </summary>
    Task<bool> TryWaitUntilAsync(
        Func<CancellationToken, Task<bool>> checkAsync,
        TimeSpan timeout,
        CancellationToken ct);
}
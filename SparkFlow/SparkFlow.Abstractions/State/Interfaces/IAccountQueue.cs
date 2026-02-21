/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/State/Interfaces/IAccountQueue.cs
 * Purpose: Core component: IAccountQueue.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.State.Interfaces;

/// <summary>
/// Fixed-order queue over enabled accounts for the global runner (sequential).
/// </summary>
public interface IAccountQueue
{
    /// <summary>Rebuild from source-of-truth accounts list (filters Enabled and sorts by Order).</summary>
    void Rebuild(IEnumerable<IAccountState> allAccounts);

    /// <summary>Returns next enabled account; null if finished/empty.</summary>
    IAccountState? Next();

    /// <summary>Peek next enabled account without consuming; null if finished/empty.</summary>
    IAccountState? Peek();

    /// <summary>Reset cursor to start.</summary>
    void Reset();

    int Count { get; }
    int Remaining { get; }

    /// <summary>Snapshot current queue content (enabled + ordered).</summary>
    IReadOnlyList<IAccountState> Snapshot();
}
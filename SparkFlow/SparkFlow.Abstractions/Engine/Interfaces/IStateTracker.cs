/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/IStateTracker.cs
 * Purpose: Core component: IStateTracker.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Tracks and exposes high-level state for a running account.
/// Lightweight abstraction used for UI badges/logging.
/// </summary>
public interface IStateTracker
{
    void SetCurrent(IAccountState? account);
    void Clear();
}
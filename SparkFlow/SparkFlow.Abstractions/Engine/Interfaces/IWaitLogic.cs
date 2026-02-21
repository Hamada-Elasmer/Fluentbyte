/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/IWaitLogic.cs
 * Purpose: Core component: IWaitLogic.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Defines the waiting/backoff strategy between repeated checks.
/// </summary>
public interface IWaitLogic
{
    /// <summary>
    /// Returns how long to wait before the next attempt.
    /// attemptNumber starts at 1.
    /// </summary>
    TimeSpan GetDelay(int attemptNumber);
}
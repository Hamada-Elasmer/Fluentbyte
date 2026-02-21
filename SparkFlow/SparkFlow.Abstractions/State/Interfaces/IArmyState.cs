/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/State/Interfaces/IArmyState.cs
 * Purpose: Core component: IArmyState.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.State.Interfaces;

public interface IArmyState
{
    int Power { get; }
}
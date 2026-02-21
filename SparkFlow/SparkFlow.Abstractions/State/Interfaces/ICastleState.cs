/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/State/Interfaces/ICastleState.cs
 * Purpose: Core component: ICastleState.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.State.Interfaces;

public interface ICastleState
{
    int Level { get; }
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Health/GameHealthSeverity.cs
 * Purpose: Library component: GameHealthSeverity.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameContracts.Health;

/// <summary>
/// Severity level for game-specific health issues.
/// Keep it aligned with Core health concepts (Blocker/Warning/Info).
/// </summary>
public enum GameHealthSeverity
{
    Info = 0,
    Warning = 1,
    Blocker = 2
}
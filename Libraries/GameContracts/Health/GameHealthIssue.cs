/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Health/GameHealthIssue.cs
 * Purpose: Library component: GameHealthIssue.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameContracts.Health;

public sealed record GameHealthIssue(
    string Id,
    GameHealthSeverity Severity,
    string Title,
    string Details,
    bool CanAutoFix = false
);
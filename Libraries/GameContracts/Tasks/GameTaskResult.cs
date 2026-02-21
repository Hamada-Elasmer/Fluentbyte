/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Tasks/GameTaskResult.cs
 * Purpose: Library component: GameTaskResult.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameContracts.Tasks;

public sealed record GameTaskResult(
    bool Success,
    string Message
);
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Capabilities/GameCapabilities.cs
 * Purpose: Library component: GameCapabilities.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameContracts.Capabilities;

public sealed class GameCapabilities
{
    public bool SupportsMultiAccounts { get; init; }
    public bool SupportsBackgroundRun { get; init; }
    public bool SupportsHealthFixes { get; init; }
}
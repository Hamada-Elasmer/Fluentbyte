/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Resources/GameResourceSnapshot.cs
 * Purpose: Library component: GameResourceSnapshot.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Generic;

namespace GameContracts.Resources;

public sealed class GameResourceSnapshot
{
    public Dictionary<GameResourceType, long> Values { get; init; } = new();
}
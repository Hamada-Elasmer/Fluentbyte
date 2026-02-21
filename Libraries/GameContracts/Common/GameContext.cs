/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/GameContracts/Common/GameContext.cs
 * Purpose: Library component: GameContext.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace GameContracts.Common;

public sealed class GameContext
{
    public string ProfileId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;

    // ✅ Needed by some game modules (detector/shutdown) when no emulator controller is provided.
    // We pass ADB serial here (e.g. "emulator-5554").
    public string? DeviceId { get; init; }
}
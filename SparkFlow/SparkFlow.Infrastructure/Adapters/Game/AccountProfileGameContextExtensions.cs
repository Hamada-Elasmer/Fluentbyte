/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Adapters/Game/AccountProfileGameContextExtensions.cs
 * Purpose: Core component: AccountProfileGameContextExtensions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Common;
using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Infrastructure.Adapters.Game;

public static class AccountProfileGameContextExtensions
{
    public static GameContext ToGameContext(this AccountProfile profile)
    {
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        return new GameContext
        {
            ProfileId = profile.Id,
            GameId = profile.GameId
        };
    }
}
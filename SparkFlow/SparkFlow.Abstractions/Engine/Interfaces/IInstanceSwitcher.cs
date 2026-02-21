/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/IInstanceSwitcher.cs
 * Purpose: Core component: IInstanceSwitcher.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Ensures the correct emulator instance is active for the provided account.
/// </summary>
public interface IInstanceSwitcher
{
    Task SwitchToAsync(IAccountState account, CancellationToken ct);
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/ITaskExecutor.cs
 * Purpose: Core component: ITaskExecutor.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */


using GameContracts.Tasks;
using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Executes a game task in a safe, profile-bound way.
/// </summary>
public interface ITaskExecutor
{
    Task<GameTaskResult> ExecuteAsync(IAccountState account, IGameTask task, CancellationToken ct);
}
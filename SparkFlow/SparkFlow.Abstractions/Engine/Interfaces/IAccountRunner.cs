/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Engine/Interfaces/IAccountRunner.cs
 * Purpose: Core component: IAccountRunner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Abstractions.Engine.Interfaces;

/// <summary>
/// Runs a single account end-to-end (attach emulator, prepare ADB, run tasks, cleanup).
/// </summary>
public interface IAccountRunner
{
    Task RunAsync(IAccountState account, CancellationToken ct);
}
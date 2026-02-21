/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Abstractions/IGlobalRunnerService.cs
 * Purpose: Core component: IGlobalRunnerService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Domain.Models;

namespace SparkFlow.Abstractions.Abstractions;


public interface IGlobalRunnerService
{
    GlobalRunnerState State { get; }
    event Action<GlobalRunnerState>? StateChanged;

    Task StartAsync(CancellationToken ct = default);

    void Pause();
    void Resume();

    Task StopAsync();
}
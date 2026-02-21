/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Runner/NullGlobalRunnerService.cs
 * Purpose: Core component: NullGlobalRunnerService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Domain.Models;

namespace SparkFlow.Engine.Runner;

/// <summary>
/// No-op implementation of IGlobalRunnerService.
/// Used when the real runner is unavailable (non-Windows or disabled).
/// </summary>
public sealed class NullGlobalRunnerService : IGlobalRunnerService
{
    public GlobalRunnerState State => GlobalRunnerState.Unavailable;

    #pragma warning disable CS0067
    public event Action<GlobalRunnerState>? StateChanged;
    #pragma warning restore CS0067

    public Task StartAsync(CancellationToken ct = default)
        => Task.CompletedTask;

    public void Pause()
    {
        // no-op
    }

    public void Resume()
    {
        // no-op
    }

    public Task StopAsync()
        => Task.CompletedTask;
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Emulator/Binding/NullProfilesAutoBinder.cs
 * Purpose: Core component: NullProfilesAutoBinder.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Services.Emulator.Binding;

namespace SparkFlow.Infrastructure.Services.Emulator.Binding;

/// <summary>
/// Platform-safe fallback.
/// Always registered so DI resolution never fails.
/// </summary>
public sealed class NullProfilesAutoBinder : IProfilesAutoBinder
{
    public Task AutoBindUnboundProfilesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
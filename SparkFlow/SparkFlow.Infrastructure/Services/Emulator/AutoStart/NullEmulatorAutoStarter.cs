/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Emulator/AutoStart/NullEmulatorAutoStarter.cs
 * Purpose: Core component: NullEmulatorAutoStarter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Services.Emulator.AutoStart;

namespace SparkFlow.Infrastructure.Services.Emulator.AutoStart;

/// <summary>
/// Platform-safe fallback.
/// Always registered so DI resolution never fails (UI stability guarantee).
/// </summary>
public sealed class NullEmulatorAutoStarter : IEmulatorAutoStarter
{
    public Task EnsureAnyDeviceReadyAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
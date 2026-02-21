/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Health/Models/HealthCheckContext.cs
 * Purpose: Core component: HealthCheckContext.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using AdbLib.Options;

namespace SparkFlow.Abstractions.Models;

/// <summary>
/// Minimal context for HealthCheck items.
/// </summary>
public sealed class HealthCheckContext
{
    public string ProfileId { get; init; } = "";

    public AdbOptions? AdbOptions { get; init; }

    /// <summary>
    /// Emulator InstanceId (string) – matches EmulatorLib.
    /// Best-effort only; ADB serial is the source of truth.
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>
    /// ADB device serial for this profile (Owner ADB).
    /// Example: "127.0.0.1:21503"
    /// </summary>
    public string? AdbSerial { get; init; }

    public override string ToString()
        => $"ProfileId='{ProfileId}', InstanceId='{InstanceId ?? "null"}', AdbSerial='{AdbSerial ?? "null"}'";
}
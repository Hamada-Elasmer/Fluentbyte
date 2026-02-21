/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.Core/Models/Accounts/DeviceBindingData.cs
 * Purpose: Core model: persisted binding data (Profile ↔ Device identity).
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 *  - GUID is SparkFlow-owned identity (primary).
 *  - AndroidId is system identity (fallback).
 * ============================================================================ */

using System.Text.Json.Serialization;

namespace SparkFlow.Domain.Models.Accounts;

public sealed class DeviceBindingData
{
    /// <summary>
    /// SparkFlow-owned stable GUID stored on-device.
    /// Primary key for binding.
    /// </summary>
    [JsonPropertyName("boundGuid")]
    public string? BoundGuid { get; set; }

    /// <summary>
    /// Android system identity used as a fallback if GUID is missing.
    /// </summary>
    [JsonPropertyName("androidId")]
    public string? AndroidId { get; set; }

    /// <summary>
    /// Last known adb serial (NOT stable). Helpful for debugging/UX only.
    /// </summary>
    [JsonPropertyName("lastKnownAdbSerial")]
    public string? LastKnownAdbSerial { get; set; }
}

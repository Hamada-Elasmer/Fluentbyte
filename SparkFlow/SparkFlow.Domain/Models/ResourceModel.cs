/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Models/ResourceModel.cs
 * Purpose: Core component: ResourceModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Text.Json.Serialization;

namespace SparkFlow.Domain.Models;

public sealed record ResourceModel
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; init; }
}
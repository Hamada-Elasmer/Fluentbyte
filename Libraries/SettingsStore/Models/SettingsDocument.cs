/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Models/SettingsDocument.cs
 * Purpose: Library component: SettingsDocument.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Newtonsoft.Json;

namespace SettingsStore.Models;

public sealed class SettingsDocument
{
    [JsonProperty("schemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonProperty("settings")]
    public AppSettings Settings { get; set; } = new();

    public const int CurrentSchemaVersion = 1;
}
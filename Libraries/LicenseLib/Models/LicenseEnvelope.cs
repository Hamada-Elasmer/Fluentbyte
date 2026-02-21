/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Models/LicenseEnvelope.cs
 * Purpose: Library component: LicenseEnvelope.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Newtonsoft.Json;

namespace LicenseLib.Models;

public sealed class LicenseEnvelope
{
    [JsonProperty("payload")]
    public LicenseLimits Payload { get; set; } = new();

    /// <summary>
    /// Base64-encoded signature over the canonical JSON of Payload.
    /// </summary>
    [JsonProperty("signature")]
    public string? Signature { get; set; }

    /// <summary>
    /// Optional: key id / issuer marker.
    /// </summary>
    [JsonProperty("kid")]
    public string? KeyId { get; set; }
}
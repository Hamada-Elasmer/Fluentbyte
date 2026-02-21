/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Providers/LicenseFileProvider.cs
 * Purpose: Library component: LicenseFileProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using LicenseLib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LicenseLib.Providers;

/// <summary>
/// File-backed license store.
/// Supports both legacy format:
///   { "KEY": { ...LicenseLimits... }, ... }
/// And envelope format:
///   { "KEY": { "payload": { ... }, "signature": "base64", "kid": "..." }, ... }
/// </summary>
public sealed class LicenseFileProvider
{
    private readonly string _filePath;

    public LicenseFileProvider(string filePath)
    {
        _filePath = filePath;
    }

    public Dictionary<string, LicenseEnvelope> LoadEnvelopes()
    {
        EnsureFileExists();

        try
        {
            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, LicenseEnvelope>(StringComparer.OrdinalIgnoreCase);

            // Detect legacy vs envelope
            var root = JToken.Parse(json);

            if (root is JObject obj)
            {
                // If first property is an object with "payload" => envelope
                var first = obj.Properties().FirstOrDefault();
                if (first?.Value is JObject firstObj && firstObj.Property("payload") != null)
                {
                    var env = obj.ToObject<Dictionary<string, LicenseEnvelope>>() 
                              ?? new Dictionary<string, LicenseEnvelope>();
                    return new Dictionary<string, LicenseEnvelope>(env, StringComparer.OrdinalIgnoreCase);
                }

                // Legacy: Dictionary<string, LicenseLimits>
                var legacy = obj.ToObject<Dictionary<string, LicenseLimits>>() 
                             ?? new Dictionary<string, LicenseLimits>();
                var converted = legacy.ToDictionary(
                    kv => kv.Key,
                    kv => new LicenseEnvelope { Payload = kv.Value },
                    StringComparer.OrdinalIgnoreCase);

                return converted;
            }

            return new Dictionary<string, LicenseEnvelope>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            // Corrupt store: return empty to keep app alive; caller can decide how to handle.
            return new Dictionary<string, LicenseEnvelope>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public void SaveEnvelopes(Dictionary<string, LicenseEnvelope> licenses)
    {
        var json = JsonConvert.SerializeObject(licenses, Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? ".");
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// Legacy API kept for convenience (payload only).
    /// </summary>
    public Dictionary<string, LicenseLimits> Load()
    {
        return LoadEnvelopes().ToDictionary(k => k.Key, v => v.Value.Payload, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Legacy API kept for convenience (payload only).
    /// </summary>
    public void Save(Dictionary<string, LicenseLimits> licenses)
    {
        var envelopes = licenses.ToDictionary(
            kv => kv.Key,
            kv => new LicenseEnvelope { Payload = kv.Value },
            StringComparer.OrdinalIgnoreCase);

        SaveEnvelopes(envelopes);
    }

    public void Reset()
    {
        SaveEnvelopes(new Dictionary<string, LicenseEnvelope>(StringComparer.OrdinalIgnoreCase));
    }

    private void EnsureFileExists()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(_filePath))
        {
            SaveEnvelopes(new Dictionary<string, LicenseEnvelope>(StringComparer.OrdinalIgnoreCase));
        }
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Internal/CanonicalJson.cs
 * Purpose: Library component: CanonicalJson.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LicenseLib.Internal;

internal static class CanonicalJson
{
    public static string SerializeCanonical(object obj)
    {
        var token = JToken.FromObject(obj, JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        }));

        var normalized = Normalize(token);
        return normalized.ToString(Formatting.None);
    }

    private static JToken Normalize(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Object => NormalizeObject((JObject)token),
            JTokenType.Array => new JArray(((JArray)token).Select(Normalize)),
            _ => token
        };
    }

    private static JObject NormalizeObject(JObject obj)
    {
        var props = obj.Properties()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => new JProperty(p.Name, Normalize(p.Value)));

        var normalized = new JObject();
        foreach (var p in props)
            normalized.Add(p);

        return normalized;
    }
}
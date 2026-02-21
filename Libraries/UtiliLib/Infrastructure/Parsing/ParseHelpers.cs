/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Infrastructure/Parsing/ParseHelpers.cs
 * Purpose: Library component: for.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace UtiliLib.Infrastructure.Parsing
{
    /// <summary>
    /// Helper class for parsing JSON and XML strings into objects.
    /// Provides extension methods for string to Stream conversion.
    /// </summary>
    public static class ParseHelpers
    {
        // ================================
        // JSON Serializer
        // ================================
        private static readonly JsonSerializer JsonSerializerInstance = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        // ================================
        // Public Extension Methods
        // ================================

        /// <summary>
        /// Convert a string to a memory stream (UTF-8 encoded).
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>MemoryStream containing the string data</returns>
        public static Stream ToStream(this string input)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Parse an XML string into an object of type T.
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="xml">XML string</param>
        /// <returns>Deserialized object or null on failure</returns>
        public static T? ParseXML<T>(this string xml) where T : class
        {
            if (string.IsNullOrWhiteSpace(xml))
                return null;

            try
            {
                using var stream = xml.ToStream();
                var serializer = new XmlSerializer(typeof(T));
                return serializer.Deserialize(stream) as T;
            }
            catch
            {
                return null; // Ignore invalid XML
            }
        }

        /// <summary>
        /// Parse a JSON string into an object of type T.
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>Deserialized object or null on failure</returns>
        public static T? ParseJSON<T>(this string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using var reader = new StringReader(json);
                using var jsonReader = new JsonTextReader(reader);
                return JsonSerializerInstance.Deserialize<T>(jsonReader);
            }
            catch
            {
                return null; // Ignore invalid JSON
            }
        }
    }
}
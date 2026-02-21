/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Files/FileService.cs
 * Purpose: Library component: FileHelper.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Newtonsoft.Json;

namespace UtiliLib.Files
{
    /// <summary>
    /// Generic file helper for reading/writing JSON files.
    /// Uses a single shared serializer for all types to avoid static generic field warnings.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize/deserialize</typeparam>
    public static class FileHelper<T>
    {
        // ================================
        // Shared JSON Serializer
        // ================================
        private static readonly JsonSerializer GlobalSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        // ================================
        // Public Methods
        // ================================

        /// <summary>
        /// Read all files with the specified extension in a folder and deserialize them to a list.
        /// </summary>
        /// <param name="folderPath">Path to folder</param>
        /// <param name="extension">File extension (default is ".json")</param>
        /// <returns>List of deserialized objects</returns>
        public static List<T> ReadFiles(string folderPath, string extension = ".json")
        {
            var result = new List<T>();

            if (!Directory.Exists(folderPath))
                return result;

            var files = Directory.GetFiles(folderPath, $"*{extension}");
            foreach (var file in files)
            {
                try
                {
                    var obj = Read(file);
                    if (obj is not null)
                        result.Add(obj);
                }
                catch
                {
                    // Ignore invalid or unreadable files
                }
            }

            return result;
        }

        /// <summary>
        /// Write an object to a JSON file.
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <param name="filePath">Path to write file</param>
        /// <param name="formatting">Formatting type (Indented by default)</param>
        public static void Write(T obj, string filePath, Formatting formatting = Formatting.Indented)
        {
            using var writer = new StreamWriter(filePath);
            using var jsonWriter = new JsonTextWriter(writer);
            jsonWriter.Formatting = formatting;
            GlobalSerializer.Serialize(jsonWriter, obj);
        }

        /// <summary>
        /// Remove a file if it exists.
        /// </summary>
        /// <param name="filePath">File path to remove</param>
        public static void RemoveFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        // ================================
        // Private Methods
        // ================================

        /// <summary>
        /// Read a single JSON file and deserialize it.
        /// Private because it's only used internally.
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Deserialized object or default if file not found</returns>
        private static T? Read(string filePath)
        {
            if (!File.Exists(filePath))
                return default;

            // Initialize objects inside 'using' to ensure disposal on exception
            using var reader = new StreamReader(filePath);
            using var jsonReader = new JsonTextReader(reader);
            return GlobalSerializer.Deserialize<T>(jsonReader);
        }
    }
}
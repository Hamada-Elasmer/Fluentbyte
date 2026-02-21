/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Utilities.cs
 * Purpose: Library component: Utilities.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace UtiliLib
{
    public static class Utilities
    {
        private static readonly Random Rng = new Random();

        public static int GetCurrentTimestamp() => (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public static long GetCurrentTimestampMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public static int GetTimestampFromDate(DateTime dateTime) =>
            (int)new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
        public static DateTime GetDateFromTimestamp(int timestamp) =>
            DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

        public static string RandomHash(int length = 8)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var buffer = new char[length];
            for (int i = 0; i < length; i++) buffer[i] = chars[Rng.Next(chars.Length)];
            return new string(buffer);
        }

        public static string GetGUID() => Guid.NewGuid().ToString();

        /// <summary>
        /// MD5 for a string (hex).
        /// </summary>
        public static string GetMd5(string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        // ----------------------------
        // Base64 <-> bytes (no Bitmap)
        // ----------------------------

        public static byte[] Base64StringToBytes(string base64)
        {
            if (base64 is null) throw new ArgumentNullException(nameof(base64));
            return Convert.FromBase64String(base64);
        }

        public static string BytesToBase64String(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            return Convert.ToBase64String(bytes);
        }

        // Keep same old method name if you have callers, but change return type:
        public static byte[] Base64StringToBitmapOld(string base64) => Base64StringToBytes(base64);

        // If you still want "BitmapToBase64String" name for compatibility, make it bytes-based:
        public static string BitmapToBase64String(byte[] imageBytes) => BytesToBase64String(imageBytes);

        // ----------------------------
        // Misc
        // ----------------------------

        public static string GetFormattedRuntimeText(DateTime dateTime) => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        public static string ConvertStringToBase64(string input) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

        public static string ConvertStringToHex(string input) =>
            BitConverter.ToString(Encoding.UTF8.GetBytes(input)).Replace("-", "");

        public static string CheckCrc32(FileInfo fileInfo)
        {
            if (fileInfo is null) throw new ArgumentNullException(nameof(fileInfo));
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found.", fileInfo.FullName);

            // CRC32 implementation (standard polynomial 0xEDB88320)
            uint crc = 0xFFFFFFFFu;
            using var stream = fileInfo.OpenRead();
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                crc ^= (byte)b;
                for (int i = 0; i < 8; i++)
                    crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320u : (crc >> 1);
            }
            crc ^= 0xFFFFFFFFu;
            return crc.ToString("X8");
        }

        public static string _RemoveAllNonNumericChars(string input, string? allowedChars = null)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));

            // NOTE: original used string += (slow). Using StringBuilder instead.
            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (char.IsDigit(c) || (allowedChars?.Contains(c) ?? false))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static bool ContainsOnlyAlphaNumericCharacters(this string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            foreach (var c in input) if (!char.IsLetterOrDigit(c)) return false;
            return true;
        }

        public static string ReadXmlFile(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            return File.ReadAllText(path);
        }

        public static string RemoveWhitespace(this string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            return string.Concat(input.Where(c => !char.IsWhiteSpace(c)));
        }

        public static string RemoveAlphaNumeric(string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            return string.Concat(input.Where(c => !char.IsLetterOrDigit(c)));
        }
    }
}
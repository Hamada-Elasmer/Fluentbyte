/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/ImageExtensions.cs
 * Purpose: Library component: ImageBytesExtensions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.IO;

namespace UtiliLib
{
    /// <summary>
    /// Core-level image helpers that are UI-framework agnostic.
    /// Works with byte[] / Stream only (no Avalonia types, no System.Drawing types).
    /// </summary>
    public static class ImageBytesExtensions
    {
        /// <summary>
        /// Creates a read-only stream view over the byte array.
        /// Caller should dispose the returned stream.
        /// </summary>
        public static Stream AsReadOnlyStream(this byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            return new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);
        }

        /// <summary>
        /// Copies all bytes from a stream into a byte array.
        /// If the stream is seekable, it will read from the beginning by default.
        /// </summary>
        public static byte[] ToByteArray(this Stream stream, bool rewindIfSeekable = true)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            long? originalPos = null;

            if (rewindIfSeekable && stream.CanSeek)
            {
                originalPos = stream.Position;
                stream.Position = 0;
            }

            try
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
            finally
            {
                if (originalPos.HasValue && stream.CanSeek)
                    stream.Position = originalPos.Value;
            }
        }

        /// <summary>
        /// Defensive clone for byte[] (helps when you want immutability semantics).
        /// </summary>
        public static byte[] CloneBytes(this byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) return Array.Empty<byte>();
            return (byte[])bytes.Clone();
        }
    }
}
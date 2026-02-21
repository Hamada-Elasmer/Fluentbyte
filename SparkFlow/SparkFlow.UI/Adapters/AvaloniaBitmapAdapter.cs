/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Adapters/AvaloniaBitmapAdapter.cs
 * Purpose: UI component: AvaloniaBitmapAdapter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.IO;
using Avalonia.Media.Imaging;

namespace SparkFlow.UI.Adapters
{
    /// <summary>
    /// UI-layer adapter: Avalonia Bitmap <-> byte[]
    /// Keep Avalonia types here only (never in Core/UtiliLib).
    /// </summary>
    public static class AvaloniaBitmapAdapter
    {
        /// <summary>
        /// Convert Avalonia Bitmap to PNG bytes.
        /// </summary>
        public static byte[] ToPngBytes(Bitmap bitmap)
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));

            using var ms = new MemoryStream();
            bitmap.Save(ms); // Avalonia saves as PNG by default
            return ms.ToArray();
        }

        /// <summary>
        /// Create Avalonia Bitmap from bytes (PNG/JPEG/etc supported by Avalonia decoder).
        /// </summary>
        public static Bitmap FromBytes(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));

            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }

        /// <summary>
        /// Create Avalonia Bitmap from stream.
        /// </summary>
        public static Bitmap FromStream(Stream stream)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            return new Bitmap(stream);
        }
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Adapters/AvaloniaImageBase64Adapter.cs
 * Purpose: UI component: AvaloniaImageBase64Adapter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.IO;
using Avalonia.Media.Imaging;
using UtiliLib;

namespace SparkFlow.UI.Adapters
{
    /// <summary>
    /// UI adapter: converts between Avalonia Bitmap and Base64 / bytes.
    /// </summary>
    public static class AvaloniaImageBase64Adapter
    {
        public static Bitmap Base64ToBitmap(string base64)
        {
            if (base64 is null) throw new ArgumentNullException(nameof(base64));
            var bytes = Utilities.Base64StringToBytes(base64);
            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }

        public static string BitmapToBase64(Bitmap bitmap)
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));
            using var ms = new MemoryStream();
            bitmap.Save(ms); // PNG
            return Utilities.BytesToBase64String(ms.ToArray());
        }

        public static byte[] BitmapToPngBytes(Bitmap bitmap)
        {
            if (bitmap is null) throw new ArgumentNullException(nameof(bitmap));
            using var ms = new MemoryStream();
            bitmap.Save(ms);
            return ms.ToArray();
        }

        public static Bitmap BytesToBitmap(byte[] bytes)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }
    }
}
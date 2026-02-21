/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Logger/UnixMsToLocalTimeStringConverter.cs
 * Purpose: UI component: UnixMsToLocalTimeStringConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SparkFlow.UI.Converters.Logger;

public sealed class UnixMsToLocalTimeStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        long ms;
        try
        {
            ms = value switch
            {
                long l => l,
                int i => i,
                string s when long.TryParse(s, out var parsed) => parsed,
                _ => 0
            };
        }
        catch
        {
            return string.Empty;
        }

        if (ms <= 0) return string.Empty;

        var dt = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime();
        return dt.ToString("yyyy-MM-dd HH:mm:ss.fff", culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("One-way converter only.");
}
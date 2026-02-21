/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Converters/Logger/LogLevelToBrushConverter.cs
 * Purpose: UI component: LogLevelToBrushConverter.
 * Notes:
 *  - Maps LogLevel values to stable UI colors.
 *  - Used to visually distinguish severity in LogsPage.
 *  - Independent from LogChannel (UI_DEBUG removed safely).
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using UtiliLib.Types;

namespace SparkFlow.UI.Converters.Logger;

public sealed class LogLevelToBrushConverter : IValueConverter
{
    // =========================
    // Stable static brushes
    // =========================
    private static readonly IBrush DebugBrush     = Brushes.LightGray;
    private static readonly IBrush InfoBrush      = Brushes.LightBlue;
    private static readonly IBrush WarningBrush   = Brushes.Orange;
    private static readonly IBrush ErrorBrush     = Brushes.IndianRed;
    private static readonly IBrush ExceptionBrush = Brushes.DarkRed;
    private static readonly IBrush DefaultBrush   = Brushes.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogLevel level)
            return DefaultBrush;

        return level switch
        {
            LogLevel.DEBUG     => DebugBrush,
            LogLevel.INFO      => InfoBrush,
            LogLevel.WARNING   => WarningBrush,
            LogLevel.ERROR     => ErrorBrush,
            LogLevel.EXCEPTION => ExceptionBrush,
            _                  => DefaultBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("LogLevelToBrushConverter is one-way only.");
}
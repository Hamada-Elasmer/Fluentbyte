/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Converters/Logger/LogChannelToBrushConverter.cs
 * Purpose: UI component: LogChannelToBrushConverter.
 * Notes:
 *  - Maps LogChannel values to stable UI colors.
 *  - Used in LogsPage for visual channel distinction.
 *  - UI_DEBUG removed → UI + DEBUG is now handled via LogLevel.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using UtiliLib.Types;

namespace SparkFlow.UI.Converters.Logger;

public sealed class LogChannelToBrushConverter : IValueConverter
{
    // =========================
    // Stable static brushes
    // =========================
    private static readonly IBrush SystemBrush       = Brushes.Gray;
    private static readonly IBrush NetworkBrush      = Brushes.Purple;
    private static readonly IBrush NotificationBrush = Brushes.CornflowerBlue;
    private static readonly IBrush UiBrush           = Brushes.LightYellow;
    private static readonly IBrush ScriptBrush       = Brushes.DarkOrange;
    private static readonly IBrush HintBrush         = Brushes.LightGreen;
    private static readonly IBrush DefaultBrush      = Brushes.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogChannel channel)
            return DefaultBrush;

        return channel switch
        {
            LogChannel.SYSTEM       => SystemBrush,
            LogChannel.NETWORK      => NetworkBrush,
            LogChannel.NOTIFICATION => NotificationBrush,
            LogChannel.UI           => UiBrush,
            LogChannel.SCRIPT       => ScriptBrush,
            LogChannel.HINT         => HintBrush,
            _                       => DefaultBrush
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("LogChannelToBrushConverter is one-way only.");
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Notification/NotifyPositionToAlignmentConverter.cs
 * Purpose: UI component: NotifyPositionToAlignmentConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using UtiliLib.Notifications;

namespace SparkFlow.UI.Converters.Notification;

public sealed class NotifyPositionToAlignmentConverter : IValueConverter
{
    // parameter: "H" or "V"
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var pos = value is NotifyPosition p ? p : NotifyPosition.TopRight;
        var axis = (parameter?.ToString() ?? "H").ToUpperInvariant();

        if (axis == "V")
        {
            return pos switch
            {
                NotifyPosition.BottomLeft or NotifyPosition.BottomRight or NotifyPosition.BottomCenter
                    => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Top
            };
        }

        return pos switch
        {
            NotifyPosition.TopLeft or NotifyPosition.BottomLeft => HorizontalAlignment.Left,
            NotifyPosition.TopCenter or NotifyPosition.BottomCenter => HorizontalAlignment.Center,
            _ => HorizontalAlignment.Right
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
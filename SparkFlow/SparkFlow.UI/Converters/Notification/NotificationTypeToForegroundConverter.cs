/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Notification/NotificationTypeToForegroundConverter.cs
 * Purpose: UI component: NotificationTypeToForegroundConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SparkFlow.UI.Converters.Notification;

public sealed class NotificationTypeToForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Brushes.White;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
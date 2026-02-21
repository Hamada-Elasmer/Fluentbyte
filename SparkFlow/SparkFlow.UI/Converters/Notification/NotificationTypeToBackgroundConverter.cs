/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Notification/NotificationTypeToBackgroundConverter.cs
 * Purpose: UI component: NotificationTypeToBackgroundConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using UtiliLib.Notifications;

namespace SparkFlow.UI.Converters.Notification;

public sealed class NotificationTypeToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var type = value is NotificationType t ? t : NotificationType.Info;

        return type switch
        {
            NotificationType.Success => new SolidColorBrush(Color.Parse("#1E6F43")),
            NotificationType.Warning => new SolidColorBrush(Color.Parse("#8A5A00")),
            NotificationType.Error   => new SolidColorBrush(Color.Parse("#7A1B1B")),
            _                        => new SolidColorBrush(Color.Parse("#1C4E80")),
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
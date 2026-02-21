/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Notification/NotificationTypeToIconConverter.cs
 * Purpose: UI component: NotificationTypeToIconConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using UtiliLib.Notifications;

namespace SparkFlow.UI.Converters.Notification;

public sealed class NotificationTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var type = value is NotificationType t ? t : NotificationType.Info;

        return type switch
        {
            NotificationType.Success => "✔",
            NotificationType.Warning => "⚠",
            NotificationType.Error   => "✖",
            _                        => "ℹ"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
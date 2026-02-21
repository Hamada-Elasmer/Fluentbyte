/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Health/HealthUiStatusToBgConverter.cs
 * Purpose: UI component: HealthUiStatusToBgConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SparkFlow.UI.ViewModels.Dialogs.Contents;

namespace SparkFlow.UI.Converters.Health;

public sealed class HealthUiStatusToBgConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = value is HealthUiStatusKind k ? k : HealthUiStatusKind.Idle;

        // Soft notification-like colors (without coupling to any notification system).
        return kind switch
        {
            HealthUiStatusKind.Ok => new SolidColorBrush(Color.Parse("#193321")),       // green-ish
            HealthUiStatusKind.Warning => new SolidColorBrush(Color.Parse("#3A2F12")),  // amber-ish
            HealthUiStatusKind.Error => new SolidColorBrush(Color.Parse("#3A1515")),    // red-ish
            HealthUiStatusKind.Running => new SolidColorBrush(Color.Parse("#132B3A")),  // blue-ish
            _ => new SolidColorBrush(Color.Parse("#202020"))
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
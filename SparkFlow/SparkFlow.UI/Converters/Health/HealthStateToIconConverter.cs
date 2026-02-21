/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Converters/Health/HealthStateToIconConverter.cs
 * Purpose: UI component: HealthStateToIconConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;
using SparkFlow.Abstractions.Models;

namespace SparkFlow.UI.Converters.Health;

public sealed class HealthStateToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not HealthCheckItemState state)
            return MaterialIconKind.HelpCircleOutline;

        return state switch
        {
            // Balanced icons (same visual weight)
            HealthCheckItemState.Pending => MaterialIconKind.ClockOutline,
            HealthCheckItemState.Running => MaterialIconKind.ProgressClock,
            HealthCheckItemState.Ok      => MaterialIconKind.CheckCircle,
            HealthCheckItemState.Warning => MaterialIconKind.AlertCircle,
            HealthCheckItemState.Error   => MaterialIconKind.CloseCircle,

            _ => MaterialIconKind.HelpCircleOutline
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
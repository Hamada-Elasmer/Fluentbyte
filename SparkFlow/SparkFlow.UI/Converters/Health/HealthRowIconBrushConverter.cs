/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Converters/Health/HealthRowIconBrushConverter.cs
 * Purpose: UI component: HealthRowIconBrushConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SparkFlow.Abstractions.Models;

namespace SparkFlow.UI.Converters.Health;

public sealed class HealthRowIconBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // value = HealthCheckItemState
        if (value is not HealthCheckItemState state)
            return GetBrushOrColor(
                       "SukiLowText",
                       "SukiText",
                       "SystemControlForegroundBaseHighBrush")
                   ?? Brushes.White;

        // ✅ Suki first (Color keys), then fallback to Fluent/System brushes
        return state switch
        {
            HealthCheckItemState.Ok =>
                GetBrushOrColor(
                    "SukiSuccessColor",
                    "SystemFillColorSuccessBrush",
                    "SystemAccentColorBrush",
                    "SystemControlForegroundAccentBrush")
                ?? Brushes.LimeGreen,

            HealthCheckItemState.Warning =>
                GetBrushOrColor(
                    "SukiWarningColor",
                    "SystemFillColorCautionBrush",
                    "SystemFillColorAttentionBrush",
                    "SystemControlForegroundBaseMediumBrush")
                ?? Brushes.Gold,

            HealthCheckItemState.Error =>
                GetBrushOrColor(
                    "SukiDangerColor",
                    "SystemFillColorCriticalBrush",
                    "SystemFillColorDangerBrush",
                    "SystemControlForegroundBaseHighBrush")
                ?? Brushes.Red,

            HealthCheckItemState.Running =>
                GetBrushOrColor(
                    "SukiInformationColor",
                    "SystemAccentColorBrush",
                    "SystemControlForegroundAccentBrush",
                    "SystemControlForegroundBaseHighBrush")
                ?? Brushes.White,

            _ =>
                GetBrushOrColor(
                    "SukiLowText",
                    "SukiText",
                    "SystemControlForegroundBaseHighBrush")
                ?? Brushes.White
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static IBrush? GetBrushOrColor(params string[] keys)
    {
        foreach (var key in keys)
        {
            var res = TryGetResource(key);
            if (res is null) continue;

            // Already a brush
            if (res is IBrush b) return b;

            // Suki often exposes Colors, not Brushes
            if (res is Color c) return new SolidColorBrush(c);
        }

        return null;
    }

    private static object? TryGetResource(string key)
    {
        try
        {
            if (Application.Current is null) return null;
            return Application.Current.TryFindResource(key, out var res) ? res : null;
        }
        catch
        {
            return null;
        }
    }
}

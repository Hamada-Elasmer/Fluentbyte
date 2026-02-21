/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Converters/Health/HealthUiStatusToIconTextConverter.cs
 * Purpose: UI component: HealthUiStatusToIconTextConverter.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SparkFlow.UI.ViewModels.Dialogs.Contents;

namespace SparkFlow.UI.Converters.Health;

public sealed class HealthUiStatusToIconTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = value is HealthUiStatusKind k ? k : HealthUiStatusKind.Idle;

        // Simple text-based icons (no spinners, no complexity).
        return kind switch
        {
            HealthUiStatusKind.Ok => "✓",
            HealthUiStatusKind.Warning => "⚠",
            HealthUiStatusKind.Error => "⛔",
            HealthUiStatusKind.Running => "⏳",
            _ => "ℹ"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
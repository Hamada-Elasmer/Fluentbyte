/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Domain/Models/Dialogs/AppDialogResult.cs
 * Purpose: Core component: AppDialogResult.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Domain.Models.Dialogs;

public sealed class AppDialogResult
{
    public bool IsAccepted { get; init; }
    public bool IsCanceled { get; init; }
    public string? Value { get; init; }

    public static AppDialogResult Closed => new()
    {
        IsAccepted = false,
        IsCanceled = true,
        Value = null
    };

    public static AppDialogResult Primary => new()
    {
        IsAccepted = true,
        IsCanceled = false,
        Value = null
    };

    public static AppDialogResult Secondary => new()
    {
        IsAccepted = false,
        IsCanceled = true,
        Value = null
    };
}
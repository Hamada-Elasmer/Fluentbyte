/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Domain/Models/Dialogs/AppDialogRequest.cs
 * Purpose: Core component: AppDialogRequest.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Domain.Models.Dialogs;

public sealed class AppDialogRequest
{
    public AppDialogKind Kind { get; init; } = AppDialogKind.Info;

    public string Title { get; init; } = string.Empty;
    public string Body  { get; init; } = string.Empty;

    // Confirm buttons
    public string YesText { get; init; } = "Yes";
    public string NoText  { get; init; } = "No";

    // Custom dialogs routing key (example: "health_check")
    public string? CustomKey { get; init; }

    // Arbitrary payload for UI layer
    public Dictionary<string, object?> Payload { get; init; } = new();
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Dialogs/IDialogService.cs
 * Purpose: Core component: IDialogService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Domain.Models.Dialogs;

namespace SparkFlow.Abstractions.Services.Dialogs;

public interface IDialogService
{
    Task<AppDialogResult> ShowAsync(AppDialogRequest request);

    Task<AppDialogResult> ShowWarningAsync(string title, string body);
    Task<AppDialogResult> ShowInfoAsync(string title, string body);
    Task<AppDialogResult> ShowConfirmAsync(
        string title,
        string body,
        string yesText = "Yes",
        string noText = "No");
}
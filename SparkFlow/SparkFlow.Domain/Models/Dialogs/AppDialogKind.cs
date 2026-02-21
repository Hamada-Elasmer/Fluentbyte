/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Domain/Models/Dialogs/AppDialogKind.cs
 * Purpose: Core component: AppDialogKind.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Domain.Models.Dialogs;

public enum AppDialogKind
{
    Warning,
    Info,
    Confirm,
    Custom
}
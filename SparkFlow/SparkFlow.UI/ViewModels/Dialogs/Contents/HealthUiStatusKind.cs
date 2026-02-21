/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Dialogs/Contents/HealthUiStatusKind.cs
 * Purpose: UI component: HealthUiStatusKind.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.UI.ViewModels.Dialogs.Contents;

public enum HealthUiStatusKind
{
    Idle,
    Running,
    Ok,
    Warning,
    Error
}
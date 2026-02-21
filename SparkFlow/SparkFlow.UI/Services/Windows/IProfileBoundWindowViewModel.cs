/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Services/Windows/IProfileBoundWindowViewModel.cs
 * Purpose: UI component: IProfileBoundWindowViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.UI.Services.Windows;

public interface IProfileBoundWindowViewModel
{
    string ProfileId { get; set; }
}
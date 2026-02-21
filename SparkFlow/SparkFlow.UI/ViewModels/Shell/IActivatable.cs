/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Shell/IActivatable.cs
 * Purpose: UI component: IActivatable.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.UI.ViewModels.Shell;

/// <summary>
/// Page/ViewModel lifecycle hooks.
/// OnEnter: called when the page becomes active.
/// OnExit: called when the page is being replaced.
/// </summary>
public interface IActivatable
{
    void OnEnter();
    void OnExit();
}
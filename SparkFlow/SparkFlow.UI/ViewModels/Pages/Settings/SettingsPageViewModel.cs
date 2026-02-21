/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Pages/Settings/SettingsPageViewModel.cs
 * Purpose: UI component: SettingsPageViewModel.
 * Notes:
 *  - Provides a support/advanced entry point to re-run the Setup Wizard.
 *  - The wizard itself does not modify system security settings automatically.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.UI.ViewModels.Shell;


namespace SparkFlow.UI.ViewModels.Pages.Settings;

public sealed class SettingsPageViewModel : ViewModelBase, IActivatable
{
    public void OnEnter()
    {
        // No-op. Reserved for future settings refresh logic.
    }

    public void OnExit()
    {
        // No-op.
    }
}

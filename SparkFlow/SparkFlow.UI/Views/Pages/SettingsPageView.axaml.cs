/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Views/Pages/SettingsPageView.axaml.cs
 * Purpose: UI component: SettingsPageView.
 * Notes:
 *  - This page hosts support / advanced actions (e.g., re-running the Setup Wizard).
 *  - The Setup Wizard should run automatically on first launch; this is a manual entry point.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SparkFlow.UI.Views.Pages;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);
}

/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Views/Pages/HomePageView.axaml.cs
 * Purpose: UI component: HomePageView.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SparkFlow.UI.ViewModels.Pages.Home;

namespace SparkFlow.UI.Views.Pages;

public partial class HomePageView : UserControl
{
    public HomePageView()
    {
        InitializeComponent();
        // Use DI to get ViewModel
        DataContext = App.ServiceProvider!
            .GetRequiredService<HomePageViewModel>();
    }
}
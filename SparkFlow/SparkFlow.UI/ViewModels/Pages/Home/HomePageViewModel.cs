/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Pages/Home/HomePageViewModel.cs
 * Purpose: UI component: HomePageViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.ObjectModel;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Pages.Home;

public sealed class HomePageViewModel : ViewModelBase, IActivatable
{
    // ===============================
    // Global App Info
    // ===============================
    public AppInfoViewModel AppInfo { get; }

    // ===============================
    // Features
    // ===============================
    public ObservableCollection<FeatureCardViewModel> Features { get; }

    // ===============================
    // Constructor (no side-effects)
    // ===============================
    public HomePageViewModel(AppInfoViewModel appInfo)
    {
        AppInfo = appInfo;

        Features = new ObservableCollection<FeatureCardViewModel>
        {
            new FeatureCardViewModel
            {
                Name = "Build Castle to Level 15",
                Description = "Automate building upgrades until level 15 with strict resource checks.",
                IsNew = true,
                IsUpdated = false,
                IsEnabled = true,
                IsSoon = false
            },
            new FeatureCardViewModel
            {
                Name = "Monster Hunting",
                Description = "Attack monsters strategically to gain resources and experience safely.",
                IsNew = false,
                IsUpdated = true,
                IsEnabled = true,
                IsSoon = false
            },
            new FeatureCardViewModel
            {
                Name = "King’s Road",
                Description = "Continue quests only when the user enables it.",
                IsNew = false,
                IsUpdated = false,
                IsEnabled = true,
                IsSoon = true
            },
            new FeatureCardViewModel
            {
                Name = "Train Troops",
                Description = "Recruit and manage your troops efficiently without manual intervention.",
                IsNew = false,
                IsUpdated = false,
                IsEnabled = true,
                IsSoon = true
            },
            new FeatureCardViewModel
            {
                Name = "Train Troops",
                Description = "Recruit and manage your troops efficiently without manual intervention.",
                IsNew = false,
                IsUpdated = false,
                IsEnabled = true,
                IsSoon = true
            }
        };
    }

    // ===============================
    // IActivatable
    // ===============================
    public void OnEnter()
    {
        // Put any page-enter logic here (refresh data, start timers, etc.)
        // Avoid Console.WriteLine in UI apps; prefer MLogger if you want.
        // Example:
        // MLogger.Instance.Info(LogChannel.UI, "[HomeVM] OnEnter");
    }

    public void OnExit()
    {
        // Cleanup page resources here (stop timers, detach events, etc.)
        // Example:
        // MLogger.Instance.Info(LogChannel.UI, "[HomeVM] OnExit");
    }
}
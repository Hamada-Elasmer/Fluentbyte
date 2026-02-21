/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Windows/Accounts/AccountDashboardWindowViewModel.cs
 * Purpose: UI component: AccountDashboardWindowViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.UI.Services.Windows;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Windows.Accounts;

public sealed class AccountDashboardWindowViewModel : ViewModelBase, IProfileBoundWindowViewModel
{
    public string ProfileId { get; set; } = string.Empty;
}
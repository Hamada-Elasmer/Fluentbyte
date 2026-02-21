/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Windows/Accounts/AccountHealthCheckWindowViewModel.cs
 * Purpose: UI component: AccountHealthCheckWindowViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using SparkFlow.Abstractions.Services.Health;
using SparkFlow.UI.Services.Windows;
using SparkFlow.UI.ViewModels.Dialogs.Contents;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Windows.Accounts;

public sealed class AccountHealthCheckWindowViewModel : ViewModelBase, IProfileBoundWindowViewModel
{
    private bool _isOpen;
    private string _profileId = "";

    public string ProfileId
    {
        get => _profileId;
        set
        {
            if (string.Equals(_profileId, value, StringComparison.OrdinalIgnoreCase)) return;

            _profileId = value;
            OnPropertyChanged();

            // If the window is open and ProfileId is now valid, trigger the check immediately.
            if (_isOpen && !string.IsNullOrWhiteSpace(_profileId))
            {
                Content.Activate(_profileId);
            }
        }
    }

    public AccountHealthCheckDialogContentViewModel Content { get; }

    public AccountHealthCheckWindowViewModel(
        IHealthCheckService health,
        Func<AccountHealthCheckDialogContentViewModel> contentFactory)
    {
        if (health is null) throw new ArgumentNullException(nameof(health));
        if (contentFactory is null) throw new ArgumentNullException(nameof(contentFactory));

        Content = contentFactory();
        Content.SetHealthService(health);
    }

    public void OnOpened()
    {
        _isOpen = true;

        // Auto-run happens here (when ProfileId is available).
        if (!string.IsNullOrWhiteSpace(ProfileId))
            Content.Activate(ProfileId);
    }

    public void OnClosed()
    {
        _isOpen = false;
        Content.Deactivate();
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Services/Windows/AccountWindowsService.cs
 * Purpose: UI component: AccountWindowsService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using Avalonia.Controls.ApplicationLifetimes;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.UI.Services.Dialogs;
using SparkFlow.UI.Views.Windows.Accounts;
using SukiUI.Dialogs;

namespace SparkFlow.UI.Services.Windows;

public sealed class AccountWindowsService(
    Func<AccountTasksWindow> tasksFactory,
    Func<AccountDashboardWindow> dashboardFactory,
    IAccountsSelector selector,
    ISukiDialogManager dialogManager,
    HealthDialogContentFactory healthFactory,
    GameDialogContentFactory gameFactory
) : IAccountWindowsService
{
    private readonly Func<AccountTasksWindow> _tasksFactory = tasksFactory;
    private readonly Func<AccountDashboardWindow> _dashboardFactory = dashboardFactory;

    private readonly IAccountsSelector _selector = selector;

    private readonly ISukiDialogManager _dialogManager = dialogManager;

    private readonly HealthDialogContentFactory _healthFactory = healthFactory;
    private readonly GameDialogContentFactory _gameFactory = gameFactory;

    public void OpenHealthCheck(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return;

        profileId = profileId.Trim();

        _selector.Select(profileId);

        var content = _healthFactory(profileId);

        _dialogManager.CreateDialog()
            .WithTitle("Health Check")
            .WithContent(content)
            .Dismiss().ByClickingBackground()
            .TryShow();
    }

    public void OpenTasks(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return;

        var w = _tasksFactory();
        ShowWithOwner(w);
    }

    public void OpenDashboard(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return;

        var w = _dashboardFactory();
        ShowWithOwner(w);
    }

    public void OpenGameInstall(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return;

        profileId = profileId.Trim();

        _selector.Select(profileId);

        var content = _gameFactory(profileId);

        _dialogManager.CreateDialog()
            .WithTitle("Game Info")
            .WithContent(content)
            .Dismiss().ByClickingBackground()
            .TryShow();
    }

    private static void ShowWithOwner(Avalonia.Controls.Window w)
    {
        var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;

        if (owner is not null) w.Show(owner);
        else w.Show();
    }
}

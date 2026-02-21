/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Shell/ShellViewModel.cs
 * Purpose: Root shell navigation ViewModel.
 * Notes:
 *  - This ViewModel controls the current visible page inside the main window.
 *  - Navigation is requested through IPageNavigationService.
 *  - Pages are created lazily using DI factories.
 * ============================================================================ */

using Avalonia.Controls;
using SparkFlow.UI.Services.Navigation;
using SparkFlow.UI.Views.Pages;
using SparkFlow.UI.Views.Pages.Accounts;
using System;
using SparkFlow.Domain.Models.Pages;

namespace SparkFlow.UI.ViewModels.Shell;

public sealed class ShellViewModel : ViewModelBase
{
    // ==========================================================
    // Navigation Service
    // ==========================================================

    private readonly IPageNavigationService _nav;

    // ==========================================================
    // Page Factories (Lazy Creation)
    // ==========================================================

    private readonly Func<HomePageView> _homeFactory;
    private readonly Func<AccountsPageView> _accountsFactory;

    // ==========================================================
    // Cached Page Instances
    // ==========================================================

    private UserControl? _homePage;
    private UserControl? _accountsPage;

    // ==========================================================
    // Current Visible Page
    // ==========================================================

    private UserControl _currentPage;

    /// <summary>
    /// The page currently displayed in the shell ContentControl.
    /// </summary>
    public UserControl CurrentPage
    {
        get => _currentPage;
        private set => RaiseAndSetIfChanged(ref _currentPage, value);
    }

    // ==========================================================
    // Constructor
    // ==========================================================

    public ShellViewModel(
        IPageNavigationService nav,
        Func<HomePageView> homeFactory,
        Func<AccountsPageView> accountsFactory)
    {
        _nav = nav;

        _homeFactory = homeFactory;
        _accountsFactory = accountsFactory;

        // Subscribe to navigation requests
        _nav.NavigationRequested += NavigateToPage;

        // Default startup page
        _currentPage = GetHomePage();
    }

    // ==========================================================
    // Page Resolution (Lazy + Cached)
    // ==========================================================

    private UserControl GetHomePage()
        => _homePage ??= _homeFactory();

    private UserControl GetAccountsPage()
        => _accountsPage ??= _accountsFactory();

    // ==========================================================
    // Navigation Handler
    // ==========================================================

    public void NavigateToPage(PagesEnum page)
    {
        var next = page switch
        {
            PagesEnum.HomePage => GetHomePage(),
            PagesEnum.AccountsPage => GetAccountsPage(),
            _ => GetHomePage()
        };

        CurrentPage = next;
    }
}

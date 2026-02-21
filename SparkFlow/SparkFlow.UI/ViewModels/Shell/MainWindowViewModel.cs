/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Shell/MainWindowViewModel.cs
 * Purpose: Root shell ViewModel for SparkFlow main window.
 * Notes:
 *  - Hosts the current page and manages sidebar navigation.
 *  - Uses IPageNavigationService for decoupled navigation requests.
 *  - Pages are LAZY-loaded (Views created only when navigated to).
 *  - Sidebar items are MODELS (not Controls) to avoid Avalonia visual-parent crashes.
 *  - No LINQ is used (indexable collections accessed directly).
 * ============================================================================ */

using Avalonia.Controls;
using Material.Icons;
using SparkFlow.UI.Services.Navigation;
using SparkFlow.UI.Services.Shell;
using SparkFlow.UI.ViewModels.Shell.Controls;
using SparkFlow.UI.Views.Pages;
using SparkFlow.UI.Views.Pages.Accounts;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System;
using System.Collections.Generic;
using SparkFlow.Domain.Models.Pages;

namespace SparkFlow.UI.ViewModels.Shell;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    // ==========================================================
    // Global Shell Controllers
    // ==========================================================

    public AppInfoViewModel AppInfo { get; }
    public AppControlViewModel AppControl { get; }

    public ISukiToastManager ToastManager { get; }
    public ISukiDialogManager DialogManager { get; }

    // ==========================================================
    // Navigation Core
    // ==========================================================

    private readonly IPageNavigationService _nav;

    // ==========================================================
    // Page Factories (Lazy Construction)
    // ==========================================================

    private readonly Func<HomePageView> _homeFactory;
    private readonly Func<AccountsPageView> _accountsFactory;
    private readonly Func<LogsPageView> _logsFactory;
    private readonly Func<SettingsPageView> _settingsFactory;

    // ==========================================================
    // Cached Pages (Single Instance Per Page)
    // ==========================================================

    private UserControl? _homePage;
    private UserControl? _accountsPage;
    private UserControl? _logsPage;
    private UserControl? _settingsPage;

    // ==========================================================
    // Sidebar Items (Models, ReadOnly)
    // ==========================================================

    public IReadOnlyList<ShellSideMenuItemModel> Pages { get; }

    // ==========================================================
    // SukiSideMenu SelectedItem
    // ==========================================================

    private ShellSideMenuItemModel? _activePage;
    private bool _syncing;

    public ShellSideMenuItemModel? ActivePage
    {
        get => _activePage;
        set
        {
            if (!RaiseAndSetIfChanged(ref _activePage, value))
                return;

            // Prevent loops when we set selection programmatically.
            if (_syncing)
                return;

            // User selection -> request navigation (keeps your architecture).
            if (value is not null)
                _nav.RequestNavigation(value.Page);
        }
    }

    // ==========================================================
    // Current Displayed Page (Right Side Content)
    // ==========================================================

    private UserControl? _currentPage;

    public UserControl? CurrentPage
    {
        get => _currentPage;
        set => RaiseAndSetIfChanged(ref _currentPage, value);
    }

    // ==========================================================
    // Constructor
    // ==========================================================

    public MainWindowViewModel(
        AppInfoViewModel appInfo,
        AppControlViewModel appControl,
        ISukiToastManager toastManager,
        ISukiDialogManager dialogManager,
        IPageNavigationService nav,
        Func<HomePageView> homeFactory,
        Func<AccountsPageView> accountsFactory,
        Func<LogsPageView> logsFactory,
        Func<SettingsPageView> settingsFactory)
    {
        AppInfo = appInfo;
        AppControl = appControl;

        ToastManager = toastManager;
        DialogManager = dialogManager;

        _nav = nav;

        _homeFactory = homeFactory;
        _accountsFactory = accountsFactory;
        _logsFactory = logsFactory;
        _settingsFactory = settingsFactory;

        Pages = new List<ShellSideMenuItemModel>
        {
            new("Home",     MaterialIconKind.Home,          PagesEnum.HomePage),
            new("Accounts", MaterialIconKind.Account,       PagesEnum.AccountsPage),
            new("Logs",     MaterialIconKind.ClipboardText, PagesEnum.LogsPage),
            new("Settings", MaterialIconKind.Cog,           PagesEnum.SettingsPage),
        };

        _nav.NavigationRequested += NavigateToPage;

        // Default startup navigation.
        NavigateToPage(PagesEnum.HomePage);
    }

    // ==========================================================
    // Lazy Page Accessors
    // ==========================================================

    private UserControl GetHomePage() => _homePage ??= _homeFactory();
    private UserControl GetAccountsPage() => _accountsPage ??= _accountsFactory();
    private UserControl GetLogsPage() => _logsPage ??= _logsFactory();
    private UserControl GetSettingsPage() => _settingsPage ??= _settingsFactory();

    private UserControl EnsurePageLoaded(PagesEnum page)
    {
        return page switch
        {
            PagesEnum.HomePage => GetHomePage(),
            PagesEnum.AccountsPage => GetAccountsPage(),
            PagesEnum.LogsPage => GetLogsPage(),
            PagesEnum.SettingsPage => GetSettingsPage(),
            _ => GetHomePage()
        };
    }

    // ==========================================================
    // Navigation Handler
    // ==========================================================

    public void NavigateToPage(PagesEnum page)
    {
        // Exit lifecycle (optional) for previous page
        ExitIfSupported(CurrentPage);

        // Load view lazily and display it
        CurrentPage = EnsurePageLoaded(page);

        // Keep sidebar selection in sync (NO LINQ)
        _syncing = true;
        try
        {
            ShellSideMenuItemModel selected = Pages[0];

            for (var i = 0; i < Pages.Count; i++)
            {
                if (Pages[i].Page == page)
                {
                    selected = Pages[i];
                    break;
                }
            }

            ActivePage = selected;
        }
        finally
        {
            _syncing = false;
        }

        // Enter lifecycle (optional) for new page
        EnterIfSupported(CurrentPage);
    }

    // ==========================================================
    // Optional Lifecycle Hooks
    // ==========================================================

    private static void EnterIfSupported(UserControl? page)
    {
        if (page?.DataContext is IActivatable a)
        {
            try { a.OnEnter(); }
            catch
            {
                // ignored
            }
        }
    }

    private static void ExitIfSupported(UserControl? page)
    {
        if (page?.DataContext is IActivatable a)
        {
            try { a.OnExit(); }
            catch
            {
                // ignored
            }
        }
    }

    // ==========================================================
    // Cleanup
    // ==========================================================

    public void Dispose()
    {
        _nav.NavigationRequested -= NavigateToPage;
    }
}

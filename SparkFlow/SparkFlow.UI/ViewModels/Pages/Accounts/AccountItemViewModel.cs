/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Pages/Accounts/AccountItemViewModel.cs
 * Purpose: UI component: AccountItemViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

// SparkFlowBot.App/ViewModels/Pages/Accounts/AccountItemViewModel.cs

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SparkFlow.UI.ViewModels.Shell;
using SparkFlow.Domain.Models.Accounts;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Pages.Accounts;

public sealed class AccountItemViewModel : ViewModelBase
{
    public AccountProfile Model { get; }

    public AccountItemViewModel(AccountProfile model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));

        OpenDashboardCommand = new RelayCommand(OpenDashboard);
        HealthCheckCommand = new RelayCommand(() => LogUiAction("HealthCheck"));
        InstallGameCommand = new RelayCommand(() => LogUiAction("InstallGame"));
        AccountSettingsCommand = new RelayCommand(() => LogUiAction("Settings"));
        DeleteAccountCommand = new RelayCommand(() => LogUiAction("Delete"));

        _runnerStateText = "Runner: Idle";
        _adbStatusText = "ADB: —";
        _instanceStatusText = "Instance: —";
        _tutorialStatusText = "Tutorial: —";
        _lastSnapshotText = "Snapshot: —";
    }

    public string Name => Model.Name;

    /// <summary>
    /// Emulator InstanceId (string) – matches EmulatorLib.
    /// In ADB-first mode it may be null (or "-1" in legacy files, normalized to null by ProfilesStore).
    /// </summary>
    public string? InstanceId => Model.InstanceId;

    public string InstanceInfo =>
        string.IsNullOrWhiteSpace(InstanceId)
            ? "Instance Default"
            : $"Farm {InstanceId}";

    public string LastActiveText => "—";
    public bool IsEnabled => Model.Active;

    public string RunnerStateText => _runnerStateText;
    private string _runnerStateText;

    public string AdbStatusText => _adbStatusText;
    private string _adbStatusText;

    public string InstanceStatusText => _instanceStatusText;
    private string _instanceStatusText;

    public string TutorialStatusText => _tutorialStatusText;
    private string _tutorialStatusText;

    public string LastSnapshotText => _lastSnapshotText;
    private string _lastSnapshotText;

    public ICommand OpenDashboardCommand { get; }
    public ICommand HealthCheckCommand { get; }
    public ICommand InstallGameCommand { get; }
    public ICommand AccountSettingsCommand { get; }
    public ICommand DeleteAccountCommand { get; }

    public event Action<AccountItemViewModel>? DashboardRequested;

    private void OpenDashboard()
    {
        DashboardRequested?.Invoke(this);
        MLogger.Instance.Info(LogChannel.UI,
            $"[UI_DEBUG] OpenDashboard requested for {Name}");
    }

    private void LogUiAction(string action)
    {
        MLogger.Instance.Debug(LogChannel.UI,
            $"[UI_DEBUG] {action} clicked for {Name}");
    }
}

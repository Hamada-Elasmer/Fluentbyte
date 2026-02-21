/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Pages/Accounts/EmulatorInstanceItemViewModel.cs
 * Purpose: UI component: EmulatorInstanceItemViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using EmulatorLib.Abstractions;
using SparkFlow.UI.ViewModels.Shell;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Pages.Accounts;

public sealed class EmulatorInstanceItemViewModel : ViewModelBase
{
    private readonly IEmulatorInstance _instance;

    public EmulatorInstanceItemViewModel(IEmulatorInstance instance)
    {
        _instance = instance ?? throw new ArgumentNullException(nameof(instance));

        HealthCheckCommand = new RelayCommand(HealthCheck);
        InstallGameCommand = new RelayCommand(InstallGame);
        AccountSettingsCommand = new RelayCommand(OpenSettings);
        OpenDashboardCommand = new RelayCommand(OpenDashboard);
        DeleteAccountCommand = new RelayCommand(DeleteAccount);
    }

    public string Name => _instance.Name;

    // InstanceId is string across the whole project (matches EmulatorLib).
    public string InstanceId => _instance.InstanceId;

    // Stable UI string (do not bind UI to EmulatorLib enum types directly).
    public string StateText => SafeStateText();

    // Keep the old display format but using InstanceId (string) instead of Id (int).
    public string InstanceInfo => $"Farm {InstanceId} _ {Name}";

    public string LastActiveText => _lastActiveText;
    private string _lastActiveText = "—";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            RaisePropertyChanged(nameof(IsEnabled));

            MLogger.Instance.Info(LogChannel.UI, $"[UI] IsEnabled({Name}) = {value}");

            EnabledChanged?.Invoke(this, value);
        }
    }
    private bool _isEnabled = true;

    public event Action<EmulatorInstanceItemViewModel, bool>? EnabledChanged;

    public ICommand HealthCheckCommand { get; }
    public ICommand InstallGameCommand { get; }
    public ICommand AccountSettingsCommand { get; }
    public ICommand OpenDashboardCommand { get; }
    public ICommand DeleteAccountCommand { get; }

    public event Action<EmulatorInstanceItemViewModel>? OpenDashboardRequested;
    public event Action<EmulatorInstanceItemViewModel>? DeleteRequested;

    private void HealthCheck()
        => MLogger.Instance.Info(LogChannel.UI, $"[UI] HealthCheck: {Name}");

    private void InstallGame()
        => MLogger.Instance.Info(LogChannel.UI, $"[UI] InstallGame: {Name}");

    private void OpenSettings()
        => MLogger.Instance.Info(LogChannel.UI, $"[UI] Settings: {Name}");

    private void OpenDashboard()
    {
        MLogger.Instance.Info(LogChannel.UI, $"[UI] OpenDashboard: {Name}");
        OpenDashboardRequested?.Invoke(this);
    }

    private void DeleteAccount()
    {
        MLogger.Instance.Info(LogChannel.UI, $"[UI] Delete: {Name}");
        DeleteRequested?.Invoke(this);
    }

    public void SetLastActive(string text)
    {
        _lastActiveText = string.IsNullOrWhiteSpace(text) ? "—" : text;
        RaisePropertyChanged(nameof(LastActiveText));
    }

    private string SafeStateText()
    {
        try
        {
            // _instance.State is still available, but its enum type/namespace may change.
            // Convert to string and keep UI stable.
            var s = _instance.State.ToString();
            return string.IsNullOrWhiteSpace(s) ? "Unknown" : s;
        }
        catch
        {
            return "Unknown";
        }
    }
}

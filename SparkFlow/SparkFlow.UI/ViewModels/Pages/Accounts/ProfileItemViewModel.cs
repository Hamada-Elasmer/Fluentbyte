/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Pages/Accounts/ProfileItemViewModel.cs
 * Purpose: UI component: ProfileItemViewModel.
 * Notes:
 *  - InstanceName is now dynamic (updates after Resolve: ONLINE/OFFLINE + Serial).
 * ============================================================================
 */

using CommunityToolkit.Mvvm.Input;
using SparkFlow.UI.Services.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Domain.Models.Accounts;
using SparkFlow.UI.ViewModels.Shell;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Pages.Accounts;

public sealed class ProfileItemViewModel : ViewModelBase
{
    private readonly IProfilesStore _store;
    private readonly IAccountWindowsService _windows;

    private readonly AccountProfile _model;

    private bool _isInstalling;

    // ✅ NEW: keep base label (instance name only) stable, and update dynamic text on top.
    public string BaseInstanceLabel { get; }

    private string _instanceName;
    public string InstanceName
    {
        get => _instanceName;
        private set
        {
            if (_instanceName == value) return;
            _instanceName = value;
            RaisePropertyChanged(nameof(InstanceName));
        }
    }

    public ProfileItemViewModel(
        IProfilesStore store,
        IAccountWindowsService windows,
        AccountProfile model,
        string instanceName)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _windows = windows ?? throw new ArgumentNullException(nameof(windows));
        _model = model ?? throw new ArgumentNullException(nameof(model));

        // ✅ FIX: Base label must be clean (no "(Checking...)")
        BaseInstanceLabel = instanceName;

        // ✅ Start in UI with Checking... (will be replaced by ONLINE/OFFLINE)
        _instanceName = $"{BaseInstanceLabel}  |  (Checking...)";

        DeleteCommand = new RelayCommand(() =>
            DeleteRequested?.Invoke(this));

        OpenHealthCheckCommand = new RelayCommand(() =>
        {
            MLogger.Instance.Info(
                LogChannel.UI,
                $"[Accounts] Open HealthCheck (Dialog) | ProfileId={Id} | InstanceId={InstanceId ?? "null"} | AdbSerial={AdbSerial ?? "null"}");

            // ✅ Unified path: open via WindowService -> AppDialogService (same animation style)
            _windows.OpenHealthCheck(Id);
        });

        OpenTasksCommand = new RelayCommand(() =>
        {
            MLogger.Instance.Info(
                LogChannel.UI,
                $"[Accounts] Open Tasks | ProfileId={Id} | InstanceId={InstanceId ?? "null"} | AdbSerial={AdbSerial ?? "null"}");

            _windows.OpenTasks(Id);
        });

        OpenDashboardCommand = new RelayCommand(() =>
        {
            MLogger.Instance.Info(
                LogChannel.UI,
                $"[Accounts] Open Dashboard | ProfileId={Id} | InstanceId={InstanceId ?? "null"} | AdbSerial={AdbSerial ?? "null"}");

            _windows.OpenDashboard(Id);
        });

        InstallGameCommand = new AsyncRelayCommand(
            InstallGameAsync,
            CanInstallGame);
    }

    public AccountProfile Model => _model;

    public ICommand OpenHealthCheckCommand { get; }
    public ICommand OpenTasksCommand { get; }
    public ICommand OpenDashboardCommand { get; }
    public IAsyncRelayCommand InstallGameCommand { get; }

    public string Id => _model.Id;
    public string Name => _model.Name;

    public string? InstanceId => _model.InstanceId;
    public string? AdbSerial => _model.AdbSerial;

    public bool Active
    {
        get => _model.Active;
        set
        {
            if (_model.Active == value)
                return;

            _model.Active = value;
            RaisePropertyChanged(nameof(Active));

            MLogger.Instance.Info(
                LogChannel.UI,
                $"[Accounts] Active -> {value} | Name='{Name}' | ProfileId={Id} | InstanceId={InstanceId ?? "null"} | AdbSerial={AdbSerial ?? "null"}");

            InstallGameCommand.NotifyCanExecuteChanged();
            _ = PersistAsync();
        }
    }

    public DateTimeOffset? LastRun => _model.LastRun;

    public string LastActiveText
        => _model.LastRun is null
            ? "Last Active: Never"
            : $"Last Active: {_model.LastRun.Value:yyyy-MM-dd HH:mm:ss}";

    public ICommand DeleteCommand { get; }
    public event Action<ProfileItemViewModel>? DeleteRequested;

    private Task PersistAsync()
        => _store.SaveAsync(_model, CancellationToken.None);

    // ✅ NEW: called by AccountsPageViewModel after Resolve
    public void UpdateConnectionStatus(bool isOnline, string? serial)
    {
        serial = string.IsNullOrWhiteSpace(serial) ? null : serial.Trim();

        var status = isOnline ? "ONLINE" : "OFFLINE";
        var serialText = serial is null ? "No Serial" : serial;

        // e.g. "leidian1 | ONLINE | 127.0.0.1:5555"
        InstanceName = $"{BaseInstanceLabel}  |  {status}  |  {serialText}";
    }

    private bool CanInstallGame()
    {
        if (_isInstalling) return false;

        // ✅ Allow opening even if not Active (user requested game dialog to open)
        if (string.IsNullOrWhiteSpace(InstanceId) && string.IsNullOrWhiteSpace(AdbSerial))
            return false;

        if (string.IsNullOrWhiteSpace(Id))
            return false;

        return true;
    }

    private Task InstallGameAsync()
    {
        if (!CanInstallGame())
        {
            MLogger.Instance.Warn(
                LogChannel.UI,
                $"[Accounts] InstallGame blocked | ProfileId={Id} | InstanceId={InstanceId ?? "null"} | AdbSerial={AdbSerial ?? "null"}");

            return Task.CompletedTask;
        }

        var opId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _isInstalling = true;
            InstallGameCommand.NotifyCanExecuteChanged();

            MLogger.Instance.Info(
                LogChannel.UI,
                $"[Accounts] Open GameInstall Dialog | OpId={opId} | ProfileId={Id} | InstanceId={InstanceId ?? "null"} | AdbSerial={AdbSerial ?? "null"} | Name='{Name}'");

            _windows.OpenGameInstall(Id);
        }
        catch (Exception ex)
        {
            MLogger.Instance.Error(
                LogChannel.UI,
                $"[Accounts] Open GameInstall Dialog FAILED | OpId={opId} | ProfileId={Id} | Error={ex}");
        }
        finally
        {
            _isInstalling = false;
            InstallGameCommand.NotifyCanExecuteChanged();
        }

        return Task.CompletedTask;
    }
}

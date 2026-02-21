/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Dialogs/Contents/AccountHealthCheckDialogContentViewModel.cs
 * Purpose: UI component: AccountHealthCheckDialogContentViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Health;
using SparkFlow.Infrastructure.Services.Health;
using SparkFlow.UI.ViewModels.Shell;
using UtiliLib;
using UtiliLib.Notifications;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Dialogs.Contents;

public sealed class AccountHealthCheckDialogContentViewModel : ViewModelBase
{
    private readonly MLogger _log;

    private IHealthCheckService? _health;
    private HealthCheckRunner? _runner;
    private CancellationTokenSource? _cts;

    private string _profileId = "";

    private string _boundProfileId = "";
    public string BoundProfileId
    {
        get => _boundProfileId;
        set
        {
            value ??= "";
            if (string.Equals(_boundProfileId, value, StringComparison.OrdinalIgnoreCase)) return;

            _boundProfileId = value;
            OnPropertyChanged();

            // Bind profile -> triggers activation + first run
            Activate(_boundProfileId);
        }
    }

    public ObservableCollection<UiHealthRow> Rows { get; } = new();
    public ObservableCollection<HealthIssueItemViewModel> Issues { get; } = new();

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            RebindCommandStates();
        }
    }

    private string _statusText = "Idle";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal)) return;
            _statusText = value ?? "";
            OnPropertyChanged();
        }
    }

    private NotificationType _statusNotifType = NotificationType.Info;
    public NotificationType StatusNotifType
    {
        get => _statusNotifType;
        private set
        {
            if (_statusNotifType == value) return;
            _statusNotifType = value;
            OnPropertyChanged();
        }
    }

    private string _lastCheckedText = "-";
    public string LastCheckedText
    {
        get => _lastCheckedText;
        private set
        {
            if (string.Equals(_lastCheckedText, value, StringComparison.Ordinal)) return;
            _lastCheckedText = value ?? "-";
            OnPropertyChanged();
        }
    }

    private int _blockers;
    public int Blockers
    {
        get => _blockers;
        private set
        {
            if (_blockers == value) return;
            _blockers = value;
            OnPropertyChanged();
        }
    }

    private int _warnings;
    public int Warnings
    {
        get => _warnings;
        private set
        {
            if (_warnings == value) return;
            _warnings = value;
            OnPropertyChanged();
        }
    }

    private int _infos;
    public int Infos
    {
        get => _infos;
        private set
        {
            if (_infos == value) return;
            _infos = value;
            OnPropertyChanged();
        }
    }

    public IAsyncRelayCommand RecheckCommand { get; }
    public IAsyncRelayCommand FixFirstCommand { get; }

    public AccountHealthCheckDialogContentViewModel()
    {
        _log = MLogger.Instance;

        // Recheck triggers a forced live run
        RecheckCommand = new AsyncRelayCommand(RecheckAsync, () => !IsBusy && IsReadyToRun());

        // Fix All (Auto fixes only) + then Recheck
        FixFirstCommand = new AsyncRelayCommand(FixAllAsync, () => !IsBusy && IsReadyToRun() && Issues.Any(i => i.CanFix));
    }

    /// <summary>
    /// Inject the runner used to build the rows list (ItemsOrdered).
    /// Must be set BEFORE BoundProfileId in dialog wiring to avoid the 3-row fallback.
    /// </summary>
    public void SetRunner(HealthCheckRunner runner)
    {
        _runner = runner;

        if (!string.IsNullOrWhiteSpace(_profileId) && !IsBusy)
        {
            BuildInitialRows();
        }

        RebindCommandStates();
    }

    /// <summary>
    /// Inject the health service. If profile is already set, trigger a live run.
    /// </summary>
    public void SetHealthService(IHealthCheckService health)
    {
        _health = health;

        if (!string.IsNullOrWhiteSpace(_profileId) && _cts is not null && !IsBusy)
        {
            _ = RunLiveAsync(force: true);
        }

        RebindCommandStates();
    }

    public void Activate(string profileId)
    {
        _profileId = profileId ?? "";
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        BuildInitialRows();

        if (!IsReadyToRun())
        {
            StatusText = "Idle";
            StatusNotifType = NotificationType.Info;
            return;
        }

        // Auto-run when opened
        _ = RunLiveAsync(force: true);
    }

    public void Deactivate()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private void BuildInitialRows()
    {
        Rows.Clear();
        Issues.Clear();

        // Build rows from runner ordered items (real full list)
        if (_runner is not null)
        {
            foreach (var it in _runner.ItemsOrdered)
                Rows.Add(UiHealthRow.Pending(it.Id, it.Title));
        }
        else
        {
            // Safe fallback so UI never crashes
            Rows.Add(UiHealthRow.Pending(HealthCheckItemId.AdbRunning, "ADB"));
            Rows.Add(UiHealthRow.Pending(HealthCheckItemId.RuntimeFolders, "RuntimeFolders"));
            Rows.Add(UiHealthRow.Pending(HealthCheckItemId.DeviceReady, "Device Ready"));
        }

        StatusText = "Idle";
        StatusNotifType = NotificationType.Info;

        LastCheckedText = "-";
        Blockers = 0;
        Warnings = 0;
        Infos = 0;

        RebindCommandStates();
    }

    private bool IsReadyToRun()
    {
        if (_health is null) return false;
        if (_cts is null) return false;
        if (string.IsNullOrWhiteSpace(_profileId)) return false;
        return true;
    }

    private Task RecheckAsync() => RunLiveAsync(force: true);

    private async Task FixAllAsync()
    {
        if (!IsReadyToRun())
        {
            StatusText = "Idle";
            StatusNotifType = NotificationType.Info;
            return;
        }

        _cts!.Token.ThrowIfCancellationRequested();

        try
        {
            IsBusy = true;
            StatusText = "Fixing...";
            StatusNotifType = NotificationType.Info;

            var res = await _health!.FixAllAutoAsync(_profileId, _cts.Token).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var t = res.Success ? NotificationType.Success : NotificationType.Warning;

                NotificationHub.Instance.Show(
                    "Health Check",
                    res.Message ?? (res.Success ? "FixAll completed." : "FixAll failed."),
                    t,
                    4500);
            });

            // Always recheck after FixAll
            await RunLiveAsync(force: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Idle";
            StatusNotifType = NotificationType.Info;
        }
        catch (Exception ex)
        {
            StatusText = $"Fix failed: {ex.Message}";
            StatusNotifType = NotificationType.Error;

            _log.Error(LogChannel.SYSTEM, $"[HealthUI] FixAll failed: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunLiveAsync(bool force)
    {
        if (!IsReadyToRun())
        {
            StatusText = "Idle";
            StatusNotifType = NotificationType.Info;
            return;
        }

        _cts!.Token.ThrowIfCancellationRequested();

        // Progress handler updates rows LIVE (Pending/Running/Ok/Warning/Error)
        var progress = new Progress<(HealthCheckItemId id, HealthCheckItemState state, string message, HealthIssue? issue)>(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var row = Rows.FirstOrDefault(x => x.Id == t.id);
                if (row is null) return;

                row.State = t.state;
                row.Message = t.message ?? "";
            });
        });

        try
        {
            IsBusy = true;
            StatusText = "Running...";
            StatusNotifType = NotificationType.Info;

            foreach (var r in Rows)
            {
                r.State = HealthCheckItemState.Pending;
                r.Message = "";
            }

            var report = force
                ? await _health!.RunLiveAsync(_profileId, progress, _cts.Token)
                : await _health!.GetOrRunLiveAsync(_profileId, progress, maxAge: TimeSpan.FromMinutes(2), ct: _cts.Token);

            ApplyReport(report);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Idle";
            StatusNotifType = NotificationType.Info;
        }
        catch (Exception ex)
        {
            StatusText = $"Failed: {ex.Message}";
            StatusNotifType = NotificationType.Error;

            _log.Error(LogChannel.SYSTEM, $"[HealthUI] Run failed: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyReport(HealthReport report)
    {
        LastCheckedText = report.CheckedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        Issues.Clear();

        foreach (var issue in report.Issues
                     .OrderByDescending(i => i.Severity)
                     .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase))
        {
            Issues.Add(new HealthIssueItemViewModel(issue));
        }

        Blockers = report.Issues.Count(i => i.Severity == HealthIssueSeverity.Blocker);
        Warnings = report.Issues.Count(i => i.Severity == HealthIssueSeverity.Warning);
        Infos = report.Issues.Count(i => i.Severity == HealthIssueSeverity.Info);

        switch (report.Status)
        {
            case SparkFlow.Domain.Models.Pages.HealthStatus.Ok:
                StatusText = "OK";
                StatusNotifType = NotificationType.Success;
                break;

            case SparkFlow.Domain.Models.Pages.HealthStatus.Warning:
                StatusText = "Warnings";
                StatusNotifType = NotificationType.Warning;
                break;

            default:
                StatusText = "Errors";
                StatusNotifType = NotificationType.Error;
                break;
        }

        foreach (var row in Rows)
        {
            var prefix = $"health.{row.Id}.";
            var rowIssues = report.Issues
                .Where(i => !string.IsNullOrWhiteSpace(i.Code) &&
                            i.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (rowIssues.Count == 0)
            {
                row.State = HealthCheckItemState.Ok;
                continue;
            }

            var worst = rowIssues.Max(i => i.Severity);

            row.State = worst switch
            {
                HealthIssueSeverity.Blocker => HealthCheckItemState.Error,
                HealthIssueSeverity.Warning => HealthCheckItemState.Warning,
                _ => HealthCheckItemState.Warning
            };

            if (string.IsNullOrWhiteSpace(row.Message))
                row.Message = rowIssues[0].Title ?? "";
        }

        RebindCommandStates();
    }

    private void RebindCommandStates()
    {
        if (RecheckCommand is AsyncRelayCommand rc)
            rc.NotifyCanExecuteChanged();

        if (FixFirstCommand is AsyncRelayCommand fc)
            fc.NotifyCanExecuteChanged();
    }
}
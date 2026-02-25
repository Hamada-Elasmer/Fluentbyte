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
            if (string.Equals(_boundProfileId, value, StringComparison.OrdinalIgnoreCase)) return;

            _boundProfileId = value;
            OnPropertyChanged();

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

            RebindCommandStatesSafe();
        }
    }

    private string _statusText = "Idle";
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal)) return;
            _statusText = value;
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
            _lastCheckedText = value;
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

        RecheckCommand = new AsyncRelayCommand(RecheckAsync, () => !IsBusy && IsReadyToRun());
        FixFirstCommand = new AsyncRelayCommand(FixAllAsync, () => !IsBusy && IsReadyToRun() && Issues.Any(i => i.CanFix));
    }

    public void SetRunner(HealthCheckRunner runner)
    {
        _runner = runner;

        if (!string.IsNullOrWhiteSpace(_profileId) && !IsBusy)
            BuildInitialRowsSafe();

        RebindCommandStatesSafe();
    }

    public void SetHealthService(IHealthCheckService health)
    {
        _health = health;

        if (!string.IsNullOrWhiteSpace(_profileId) && _cts is not null && !IsBusy)
            _ = RunLiveAsync(force: true);

        RebindCommandStatesSafe();
    }

    public void Activate(string profileId)
    {
        _profileId = profileId; // profileId هنا non-nullable
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        BuildInitialRowsSafe();

        if (!IsReadyToRun())
        {
            _ = SetStatusUiAsync("Idle", NotificationType.Info);
            return;
        }

        _ = RunLiveAsync(force: true);
    }

    public void Deactivate()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private void BuildInitialRowsSafe()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Rows.Clear();
            Issues.Clear();

            if (_runner is not null)
            {
                foreach (var it in _runner.ItemsOrdered)
                    Rows.Add(UiHealthRow.Pending(it.Id, it.Title));
            }
            else
            {
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
        });
    }

    private bool IsReadyToRun()
    {
        if (_health is null) return false;
        if (_cts is null) return false;
        if (string.IsNullOrWhiteSpace(_profileId)) return false;
        return true;
    }

    private global::System.Threading.Tasks.Task RecheckAsync() => RunLiveAsync(force: true);

    private async global::System.Threading.Tasks.Task FixAllAsync()
    {
        if (!IsReadyToRun())
        {
            await SetStatusUiAsync("Idle", NotificationType.Info);
            return;
        }

        _cts!.Token.ThrowIfCancellationRequested();

        try
        {
            await SetBusyUiAsync(true);
            await SetStatusUiAsync("Fixing...", NotificationType.Info);

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

            await RunLiveAsync(force: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await SetStatusUiAsync("Idle", NotificationType.Info);
        }
        catch (Exception ex)
        {
            await SetStatusUiAsync($"Fix failed: {ex.Message}", NotificationType.Error);
            _log.Error(LogChannel.SYSTEM, $"[HealthUI] FixAll failed: {ex}");
        }
        finally
        {
            await SetBusyUiAsync(false);
        }
    }

    private async global::System.Threading.Tasks.Task RunLiveAsync(bool force)
    {
        if (!IsReadyToRun())
        {
            await SetStatusUiAsync("Idle", NotificationType.Info);
            return;
        }

        _cts!.Token.ThrowIfCancellationRequested();

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
            await SetBusyUiAsync(true);
            await SetStatusUiAsync("Running...", NotificationType.Info);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var r in Rows)
                {
                    r.State = HealthCheckItemState.Pending;
                    r.Message = "";
                }
            });

            var report = force
                ? await _health!.RunLiveAsync(_profileId, progress, _cts.Token).ConfigureAwait(false)
                : await _health!.GetOrRunLiveAsync(_profileId, progress, maxAge: TimeSpan.FromMinutes(2), ct: _cts.Token).ConfigureAwait(false);

            await ApplyReportUiAsync(report);
        }
        catch (OperationCanceledException)
        {
            await SetStatusUiAsync("Idle", NotificationType.Info);
        }
        catch (Exception ex)
        {
            await SetStatusUiAsync($"Failed: {ex.Message}", NotificationType.Error);
            _log.Error(LogChannel.SYSTEM, $"[HealthUI] Run failed: {ex}");
        }
        finally
        {
            await SetBusyUiAsync(false);
        }
    }

    private async global::System.Threading.Tasks.Task ApplyReportUiAsync(HealthReport report)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
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

            StatusText = report.Status switch
            {
                SparkFlow.Domain.Models.Pages.HealthStatus.Ok => "OK",
                SparkFlow.Domain.Models.Pages.HealthStatus.Warning => "Warnings",
                _ => "Errors"
            };

            StatusNotifType = report.Status switch
            {
                SparkFlow.Domain.Models.Pages.HealthStatus.Ok => NotificationType.Success,
                SparkFlow.Domain.Models.Pages.HealthStatus.Warning => NotificationType.Warning,
                _ => NotificationType.Error
            };

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

                row.State = worst == HealthIssueSeverity.Blocker
                    ? HealthCheckItemState.Error
                    : HealthCheckItemState.Warning;

                if (string.IsNullOrWhiteSpace(row.Message))
                    row.Message = rowIssues[0].Title ?? "";
            }

            RebindCommandStates();
        });
    }

    private async global::System.Threading.Tasks.Task SetBusyUiAsync(bool value)
        => await Dispatcher.UIThread.InvokeAsync(() => IsBusy = value);

    private async global::System.Threading.Tasks.Task SetStatusUiAsync(string text, NotificationType type)
        => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusText = text;
            StatusNotifType = type;
        });

    private void RebindCommandStatesSafe()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RebindCommandStates();
            return;
        }

        Dispatcher.UIThread.Post(RebindCommandStates);
    }

    private void RebindCommandStates()
    {
        if (RecheckCommand is AsyncRelayCommand rc)
            rc.NotifyCanExecuteChanged();

        if (FixFirstCommand is AsyncRelayCommand fc)
            fc.NotifyCanExecuteChanged();
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Pages/LogsPageViewModel.cs
 * Purpose: UI component: LogsPageViewModel.
 * Notes:
 *  - Binds to ILogHub (central in-memory log store).
 *  - Supports:
 *      - Fast-path append filtering (on new log entries)
 *      - Throttled rebuild for resets/removes/replaces or filter changes
 *      - Dedup window to avoid log spam in UI
 *      - Export filtered logs to a text file
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Threading;
using SparkFlow.Abstractions.Services.Logging;
using SparkFlow.UI.Utils;
using SparkFlow.UI.ViewModels.Shell;
using UtiliLib;
using UtiliLib.Models;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Pages;

public sealed class LogsPageViewModel : ViewModelBase, IActivatable
{
    // Logs come from the central hub (independent store).
    public ObservableCollection<LogEntry> Logs { get; }

    // UI-bound filtered view
    public ObservableCollection<LogEntry> FilteredLogs { get; } = new();

    // UI can subscribe to request auto-scroll behavior
    public event EventHandler<LogEntry>? RequestScrollToEnd;

    // UI dropdown options
    public IReadOnlyList<LogLevel?> LevelOptions { get; } =
        new LogLevel?[] { null, LogLevel.INFO, LogLevel.WARNING, LogLevel.ERROR, LogLevel.DEBUG, LogLevel.EXCEPTION };

    public IReadOnlyList<LogChannel?> ChannelOptions { get; } =
        new LogChannel?[] { null, LogChannel.SYSTEM, LogChannel.NETWORK, LogChannel.NOTIFICATION, LogChannel.UI, LogChannel.SCRIPT, LogChannel.HINT };

    private LogLevel? _selectedLevel;
    public LogLevel? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (_selectedLevel == value) return;
            _selectedLevel = value;
            OnPropertyChanged();
            ScheduleRebuildFiltered();
        }
    }

    private LogChannel? _selectedChannel;
    public LogChannel? SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (_selectedChannel == value) return;
            _selectedChannel = value;
            OnPropertyChanged();
            ScheduleRebuildFiltered();
        }
    }

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            ScheduleRebuildFiltered();
        }
    }

    // Direct filters for Run/Profile
    private string? _runIdFilter;
    public string? RunIdFilter
    {
        get => _runIdFilter;
        set
        {
            if (_runIdFilter == value) return;
            _runIdFilter = value;
            OnPropertyChanged();
            ScheduleRebuildFiltered();
        }
    }

    private string? _profileIdFilter;
    public string? ProfileIdFilter
    {
        get => _profileIdFilter;
        set
        {
            if (_profileIdFilter == value) return;
            _profileIdFilter = value;
            OnPropertyChanged();
            ScheduleRebuildFiltered();
        }
    }

    private bool _autoScroll = true;
    public bool AutoScroll
    {
        get => _autoScroll;
        set
        {
            if (_autoScroll == value) return;
            _autoScroll = value;
            OnPropertyChanged();
        }
    }

    public string StatsText => $"Showing {FilteredLogs.Count} / {Logs.Count}";

    public ICommand RefreshLogsCommand { get; }
    public ICommand ClearLogsCommand { get; }
    public ICommand ExportLogsCommand { get; }

    // Dedup window (prevents UI spam)
    private const int DuplicateWindowMs = 400;

    private string? _lastText;
    private LogLevel _lastLevel;
    private LogChannel _lastChannel;
    private int _lastSession;
    private long _lastTimestamp;
    private string? _lastRunId;
    private string? _lastProfileId;

    private readonly ILogHub _hub;

    // Throttled rebuild timer
    private readonly DispatcherTimer _rebuildTimer;

    public LogsPageViewModel(ILogHub hub)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));

        // Bind to hub entries
        Logs = _hub.Entries;

        RefreshLogsCommand = new RelayCommand(_ => RebuildFilteredNow());
        ClearLogsCommand = new RelayCommand(_ => ClearLogs());
        ExportLogsCommand = new RelayCommand(_ => ExportLogs());

        Logs.CollectionChanged += OnLogsCollectionChanged;
        FilteredLogs.CollectionChanged += (_, _) => NotifyStatsChanged();

        // Build a stable rebuild timer once
        _rebuildTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _rebuildTimer.Tick += RebuildTimerOnTick;

        // Initial build
        RebuildFilteredNow();
        NotifyStatsChanged();
    }

    // =========================
    // IActivatable
    // =========================
    public void OnEnter()
        => MLogger.Instance.Info(LogChannel.SYSTEM, "Logs page entered.");

    public void OnExit()
        => MLogger.Instance.Info(LogChannel.SYSTEM, "Logs page exited.");

    // =========================
    // Append filtering (fast path)
    // =========================
    private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyStatsChanged();

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (var obj in e.NewItems)
            {
                if (obj is not LogEntry entry)
                    continue;

                // Dedup window (prevents spam)
                if (IsDuplicate(entry))
                    continue;

                Remember(entry);

                // Fast path: append if matches current filters
                if (MatchesFilters(entry))
                {
                    FilteredLogs.Add(entry);

                    if (AutoScroll)
                        RequestScrollToEnd?.Invoke(this, entry);
                }
            }

            return;
        }

        // For resets/removes/replaces -> rebuild once (safe)
        ScheduleRebuildFiltered();
    }

    private bool MatchesFilters(LogEntry x)
    {
        if (SelectedLevel.HasValue && x.Level != SelectedLevel.Value)
            return false;

        if (SelectedChannel.HasValue && x.Channel != SelectedChannel.Value)
            return false;

        if (!string.IsNullOrWhiteSpace(RunIdFilter))
        {
            var r = RunIdFilter.Trim();
            if (string.IsNullOrWhiteSpace(x.RunId) || !x.RunId.Contains(r, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(ProfileIdFilter))
        {
            var p = ProfileIdFilter.Trim();
            if (string.IsNullOrWhiteSpace(x.ProfileId) || !x.ProfileId.Contains(p, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            if (!(x.Text ?? string.Empty).Contains(s, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    // =========================
    // Throttled rebuild
    // =========================
    private void ScheduleRebuildFiltered()
    {
        _rebuildTimer.Stop();
        _rebuildTimer.Start();
    }

    private void RebuildTimerOnTick(object? sender, EventArgs e)
    {
        _rebuildTimer.Stop();
        RebuildFilteredNow();
    }

    private void RebuildFilteredNow()
    {
        // Rebuild on UI thread (safe)
        Dispatcher.UIThread.Post(() =>
        {
            FilteredLogs.Clear();

            // NOTE: We iterate in existing order. If you need strict ordering by Timestamp,
            // you can switch to: foreach (var item in Logs.OrderBy(x => x.Timestamp))
            foreach (var item in Logs)
            {
                if (MatchesFilters(item))
                    FilteredLogs.Add(item);
            }

            NotifyStatsChanged();
        });
    }

    // =========================
    // Dedup
    // =========================
    private bool IsDuplicate(LogEntry entry)
    {
        if (_lastText is null) return false;

        if (_lastText == entry.Text &&
            _lastLevel == entry.Level &&
            _lastChannel == entry.Channel &&
            _lastSession == entry.SessionId &&
            string.Equals(_lastRunId ?? "", entry.RunId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_lastProfileId ?? "", entry.ProfileId, StringComparison.OrdinalIgnoreCase))
        {
            var dt = entry.Timestamp - _lastTimestamp;
            return dt >= 0 && dt <= DuplicateWindowMs;
        }

        return false;
    }

    private void Remember(LogEntry entry)
    {
        _lastText = entry.Text;
        _lastLevel = entry.Level;
        _lastChannel = entry.Channel;
        _lastSession = entry.SessionId;
        _lastTimestamp = entry.Timestamp;
        _lastRunId = entry.RunId;
        _lastProfileId = entry.ProfileId;
    }

    // =========================
    // Actions
    // =========================
    private void ClearLogs()
    {
        _hub.Clear();
        FilteredLogs.Clear();

        _lastText = null;
        _lastTimestamp = 0;
        _lastSession = 0;
        _lastChannel = default;
        _lastLevel = default;
        _lastRunId = null;
        _lastProfileId = null;

        NotifyStatsChanged();
    }

    private void ExportLogs()
    {
        try
        {
            // Export folder (more professional than root)
            var baseDir = AppContext.BaseDirectory;
            var exportDir = Path.Combine(baseDir, "runtime", "logs", "exports");
            Directory.CreateDirectory(exportDir);

            var fileName = $"logs_export_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(exportDir, fileName);

            var lines = FilteredLogs.Select(l =>
            {
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(l.Timestamp).ToLocalTime();
                var run = string.IsNullOrWhiteSpace(l.RunId) ? "-" : l.RunId;
                var prof = string.IsNullOrWhiteSpace(l.ProfileId) ? "-" : l.ProfileId;

                return $"{dt:yyyy-MM-dd HH:mm:ss.fff} | {l.Level} | {l.Channel} | Run:{run} | Session:{l.SessionId} | Profile:{prof} | {l.Text}";
            });

            File.WriteAllLines(path, lines);

            MLogger.Instance.Info(LogChannel.SYSTEM, $"Logs exported: {path}");
        }
        catch (Exception ex)
        {
            MLogger.Instance.Exception(LogChannel.SYSTEM, ex, "ExportLogs failed");
        }
    }

    private void NotifyStatsChanged()
        => OnPropertyChanged(nameof(StatsText));
}
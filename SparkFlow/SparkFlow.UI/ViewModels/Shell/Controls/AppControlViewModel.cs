/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Shell/Controls/AppControlViewModel.cs
 * Purpose: UI component: AppControlViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 *  - Notifications are handled via Suki ToastHost (ISukiToastManager) instead of NotificationHub.
 *  - Toasts use built-in timed dismiss to show the standard Suki toast progress bar.
 * ============================================================================ */

using System;
using System.Threading;
using System.Windows.Input;
using Avalonia.Threading;
using SettingsStore.Interfaces;
using SettingsStore.Models;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Domain.Models;
using SparkFlow.UI.Utils;
using SukiUI.Toasts;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Shell.Controls
{
    public class AppControlViewModel : ViewModelBase
    {
        private readonly IGlobalRunnerService _runner;
        private readonly IAccountsSelector _accounts;
        private readonly ISettingsAccessor _settings;
        private readonly ISukiToastManager _toasts;

        // ==========================================================
        // Runner State Tracking (to avoid toast spam)
        // ==========================================================

        private GlobalRunnerState _lastToastState = GlobalRunnerState.Idle;

        #region State Flags

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (RaiseAndSetIfChanged(ref _isRunning, value))
                {
                    RaisePauseTextChanged();
                    RefreshCommands();
                }
            }
        }

        private bool _isPaused;
        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (RaiseAndSetIfChanged(ref _isPaused, value))
                {
                    RaisePauseTextChanged();
                    RefreshCommands();
                }
            }
        }

        private bool _autoRestart;
        public bool AutoRestart
        {
            get => _autoRestart;
            set
            {
                if (!RaiseAndSetIfChanged(ref _autoRestart, value))
                    return;

                // Persist the toggle so the Core runner can read it.
                var s = (AppSettings)_settings.Current;
                s.AutoRestartEnabled = value;
                _settings.Save();

                // UX: toast on toggle change.
                QueueToast(
                    "Runner",
                    value ? "Auto-Restart enabled." : "Auto-Restart disabled.",
                    DefaultToastMs);
            }
        }

        #endregion

        #region Visibility

        private bool _startVisible = true;
        public bool StartVisible
        {
            get => _startVisible;
            private set => RaiseAndSetIfChanged(ref _startVisible, value);
        }

        private bool _pauseVisible;
        public bool PauseVisible
        {
            get => _pauseVisible;
            private set => RaiseAndSetIfChanged(ref _pauseVisible, value);
        }

        private bool _stopVisible;
        public bool StopVisible
        {
            get => _stopVisible;
            private set => RaiseAndSetIfChanged(ref _stopVisible, value);
        }

        #endregion

        #region Commands / Text

        public string PauseButtonText => IsPaused ? "Resume" : "Pause";

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }

        #endregion

        // ==========================================================
        // Toast Durations (ms)
        // ==========================================================

        private const int DefaultToastMs = 3500;
        private const int WarningToastMs = 5000;
        private const int ErrorToastMs = 6000;

        public AppControlViewModel(
            IGlobalRunnerService runner,
            IAccountsSelector accounts,
            ISettingsAccessor settings,
            ISukiToastManager toasts)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));

            StartCommand = new RelayCommand(_ => Start(), _ => CanStart());
            PauseCommand = new RelayCommand(_ => TogglePause(), _ => CanPause());
            StopCommand = new RelayCommand(_ => Stop(), _ => CanStop());

            // Initial sync
            ApplyRunnerState(_runner.State);

            // Persisted toggle
            AutoRestart = ((AppSettings)_settings.Current).AutoRestartEnabled;

            _runner.StateChanged += Runner_StateChanged;
        }

        #region CanExecute

        private bool CanStart()
            => _runner.State is GlobalRunnerState.Idle or GlobalRunnerState.Faulted;

        private bool CanPause()
            => _runner.State is GlobalRunnerState.Running or GlobalRunnerState.Paused;

        private bool CanStop()
            => _runner.State is GlobalRunnerState.Running
                or GlobalRunnerState.Paused
                or GlobalRunnerState.Stopping;

        #endregion

        #region Actions

        private async void Start()
        {
            try
            {
                var enabled = await _accounts
                    .GetEnabledOrderedAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                if (enabled.Count == 0)
                {
                    QueueToast(
                        "Runner",
                        "⚠️ No enabled accounts found. Enable at least one profile then start again.",
                        WarningToastMs);

                    UtiliLib.MLogger.Instance.Warn(
                        LogChannel.UI,
                        "[UI] Start blocked: No enabled accounts.",
                        0);

                    return;
                }

                QueueToast("Runner", "Starting...", DefaultToastMs);

                _ = _runner.StartAsync();
            }
            catch (Exception ex)
            {
                QueueToast("Runner", $"Start failed: {ex.Message}", ErrorToastMs);

                UtiliLib.MLogger.Instance.Exception(
                    LogChannel.UI,
                    ex,
                    "[UI] Start failed");
            }
        }

        private void TogglePause()
        {
            try
            {
                if (_runner.State == GlobalRunnerState.Running)
                {
                    _runner.Pause();
                    QueueToast("Runner", "Paused.", DefaultToastMs);
                }
                else if (_runner.State == GlobalRunnerState.Paused)
                {
                    _runner.Resume();
                    QueueToast("Runner", "Resumed.", DefaultToastMs);
                }
            }
            catch (Exception ex)
            {
                QueueToast("Runner", $"Pause/Resume failed: {ex.Message}", ErrorToastMs);

                UtiliLib.MLogger.Instance.Exception(
                    LogChannel.UI,
                    ex,
                    "[UI] Pause/Resume failed");
            }
        }

        private void Stop()
        {
            try
            {
                if (_runner.State == GlobalRunnerState.Stopping)
                {
                    QueueToast("Runner", "Already stopping...", DefaultToastMs);
                    return;
                }

                QueueToast("Runner", "Stopping...", DefaultToastMs);

                // 🔥 UI reacts immediately
                ApplyRunnerState(GlobalRunnerState.Stopping);

                _ = _runner.StopAsync();
            }
            catch (Exception ex)
            {
                QueueToast("Runner", $"Stop failed: {ex.Message}", ErrorToastMs);

                UtiliLib.MLogger.Instance.Exception(
                    LogChannel.UI,
                    ex,
                    "[UI] Stop failed");
            }
        }

        #endregion

        #region Runner State Sync

        private void Runner_StateChanged(GlobalRunnerState state)
        {
            // Always marshal to UI thread
            Dispatcher.UIThread.Post(() => ApplyRunnerState(state));
        }

        private void ApplyRunnerState(GlobalRunnerState state)
        {
            IsRunning = state is GlobalRunnerState.Running
                or GlobalRunnerState.Paused
                or GlobalRunnerState.Stopping;

            IsPaused = state is GlobalRunnerState.Paused;

            if (state == GlobalRunnerState.Faulted)
            {
                IsRunning = false;
                IsPaused = false;
            }

            // IMPORTANT: use passed state (not _runner.State)
            UpdateVisibility(state);
            RefreshCommands();

            // Toasts for key state transitions (avoid spam)
            RaiseStateToastIfNeeded(state);
        }

        private void UpdateVisibility(GlobalRunnerState state)
        {
            StartVisible = state is GlobalRunnerState.Idle or GlobalRunnerState.Faulted;
            PauseVisible = state is GlobalRunnerState.Running or GlobalRunnerState.Paused;
            StopVisible = state is GlobalRunnerState.Running
                or GlobalRunnerState.Paused
                or GlobalRunnerState.Stopping;
        }

        #endregion

        #region Toast Helpers

        private void QueueToast(string title, string message, int ms)
        {
            // Ensure toast queueing always happens on UI thread.
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _toasts.CreateToast()
                        .WithTitle(title)
                        .WithContent(message)
                        .Dismiss()
                        .After(TimeSpan.FromMilliseconds(ms)) // ✅ built-in Suki toast progress + auto close
                        .Queue();
                }
                catch
                {
                    // ignored (never crash UI for toast failures)
                }
            });
        }

        private void RaiseStateToastIfNeeded(GlobalRunnerState state)
        {
            if (state == _lastToastState)
                return;

            _lastToastState = state;

            // Keep state-to-toast mapping minimal and user-friendly.
            if (state == GlobalRunnerState.Running)
            {
                QueueToast("Runner", "Running.", DefaultToastMs);
                return;
            }

            if (state == GlobalRunnerState.Paused)
            {
                QueueToast("Runner", "Paused.", DefaultToastMs);
                return;
            }

            if (state == GlobalRunnerState.Stopping)
            {
                QueueToast("Runner", "Stopping...", DefaultToastMs);
                return;
            }

            if (state == GlobalRunnerState.Idle)
            {
                QueueToast("Runner", "Idle.", DefaultToastMs);
                return;
            }

            if (state == GlobalRunnerState.Faulted)
            {
                QueueToast("Runner", "❌ Faulted. Check Logs for details.", ErrorToastMs);
                return;
            }
        }

        #endregion

        #region Helpers

        private void RaisePauseTextChanged()
        {
            RaisePropertyChanged(nameof(PauseButtonText));
        }

        private void RefreshCommands()
        {
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}

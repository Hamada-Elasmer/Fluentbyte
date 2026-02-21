/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Shell/ViewModelBase.cs
 * Purpose: Base class for all SparkFlow UI ViewModels.
 * Notes:
 *  - Provides INotifyPropertyChanged support.
 *  - Provides logging shortcuts.
 *  - Provides shared command helpers.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Platform;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Shell;

public class ViewModelBase : INotifyPropertyChanged
{
    // ===============================
    // PropertyChanged Event
    // ===============================

    public event PropertyChangedEventHandler? PropertyChanged;

    // ===============================
    // Logger Shortcuts
    // ===============================

    protected MLogger Log => MLogger.Instance;

    protected void LInfo(LogChannel ch, string msg, int sessionId = 0)
        => Log.Info(ch, msg, sessionId);

    protected void LWarn(LogChannel ch, string msg, int sessionId = 0)
        => Log.Warn(ch, msg, sessionId);

    protected void LError(LogChannel ch, string msg, int sessionId = 0)
        => Log.Error(ch, msg, sessionId);

    protected void LEx(LogChannel ch, Exception ex, string ctx = "", int sessionId = 0)
        => Log.Exception(ch, ex, ctx, sessionId);

    // ===============================
    // Resource Loader
    // ===============================

    protected string GetAssemblyResource(string name)
    {
        using var stream = AssetLoader.Open(new Uri(name));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ===============================
    // Property Change Helpers
    // ===============================

    protected bool RaiseAndSetIfChanged<T>(
        ref T field,
        T value,
        [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        return false;
    }

    protected void RaisePropertyChanged(string propName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    protected void OnPropertyChanged([CallerMemberName] string name = "")
        => RaisePropertyChanged(name);

    // ===============================
    // Command Helper (Project Standard)
    // ===============================

    /// <summary>
    /// Creates a simple ICommand for UI bindings.
    /// Eliminates the need to define command classes inside each ViewModel.
    /// </summary>
    protected ICommand Cmd(Action execute, Func<bool>? canExecute = null)
        => new SimpleCommand(execute, canExecute);

    /// <summary>
    /// Minimal ICommand implementation used across SparkFlow UI.
    /// </summary>
    private sealed class SimpleCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public SimpleCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
            => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
            => _execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

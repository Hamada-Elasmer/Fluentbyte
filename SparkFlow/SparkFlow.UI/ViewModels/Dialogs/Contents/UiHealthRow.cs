/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Dialogs/Contents/UiHealthRow.cs
 * Purpose: UI component: UiHealthRow.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Dialogs.Contents;

public sealed class UiHealthRow : ViewModelBase
{
    public HealthCheckItemId Id { get; }
    public string Title { get; }

    private HealthCheckItemState _state;
    public HealthCheckItemState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
        }
    }

    private string _message = "";
    public string Message
    {
        get => _message;
        set
        {
            if (string.Equals(_message, value)) return;
            _message = value ?? "";
            OnPropertyChanged();
        }
    }

    private UiHealthRow(HealthCheckItemId id, string title, HealthCheckItemState state, string message)
    {
        Id = id;
        Title = title;
        _state = state;
        _message = message ?? "";
    }

    public static UiHealthRow Pending(HealthCheckItemId id, string title)
        => new(id, title, HealthCheckItemState.Pending, "");
}
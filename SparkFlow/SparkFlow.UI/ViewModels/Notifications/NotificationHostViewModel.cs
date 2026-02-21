/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Notifications/NotificationHostViewModel.cs
 * Purpose: UI component: NotificationHostViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.ObjectModel;
using System.Windows.Input;
using SparkFlow.UI.ViewModels.Shell;
using SparkFlow.UI.Utils;
using UtiliLib.Notifications;

namespace SparkFlow.UI.ViewModels.Notifications;

public sealed class NotificationHostViewModel : ViewModelBase
{
    public ObservableCollection<NotificationItem> Items { get; } = new();

    public int MaxVisible { get; set; } = 5;

    public ICommand CloseCommand { get; }

    public NotificationHostViewModel()
    {
        CloseCommand = new RelayCommand(p =>
        {
            if (p is NotificationItem item)
                Close(item);
        });
    }

    public void Add(NotificationItem item)
    {
        while (Items.Count >= MaxVisible)
            Items.RemoveAt(0);

        Items.Add(item);
    }

    public void Close(NotificationItem item)
    {
        if (Items.Contains(item))
            Items.Remove(item);
    }
}
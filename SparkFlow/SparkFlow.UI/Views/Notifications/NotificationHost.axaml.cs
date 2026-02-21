/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Views/Notifications/NotificationHost.axaml.cs
 * Purpose: UI component: NotificationHost.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SparkFlow.UI.ViewModels.Notifications;
using UtiliLib.Notifications;

namespace SparkFlow.UI.Views.Notifications;

public partial class NotificationHost : UserControl
{
    private readonly NotificationHostViewModel _vm;

    // So XAML can bind to CloseCommand from the root view model.
    public ICommand CloseCommand => _vm.CloseCommand;

    public NotificationHost()
    {
        InitializeComponent();

        _vm = (NotificationHostViewModel)DataContext!;

        ApplyPosition(NotificationHub.Instance.Position);

        NotificationHub.Instance.PositionChanged += OnPositionChanged;
        NotificationHub.Instance.Notified += OnNotified;
    }

    // IMPORTANT: do not rely on the XAML generator.
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnNotified(NotificationItem item, int durationMs)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _vm.Add(item);

            var id = item.Id;

            DispatcherTimer.RunOnce(() =>
            {
                var existing = _vm.Items.FirstOrDefault(x => x.Id == id);
                if (existing is not null)
                    _vm.Close(existing);

            }, TimeSpan.FromMilliseconds(Math.Max(500, durationMs)));
        });
    }

    private void OnPositionChanged(NotifyPosition pos)
        => Dispatcher.UIThread.Post(() => ApplyPosition(pos));

    private void ApplyPosition(NotifyPosition pos)
    {
        (HorizontalAlignment, VerticalAlignment) = pos switch
        {
            NotifyPosition.TopLeft => (Avalonia.Layout.HorizontalAlignment.Left, Avalonia.Layout.VerticalAlignment.Top),
            NotifyPosition.TopRight => (Avalonia.Layout.HorizontalAlignment.Right, Avalonia.Layout.VerticalAlignment.Top),
            NotifyPosition.BottomLeft => (Avalonia.Layout.HorizontalAlignment.Left, Avalonia.Layout.VerticalAlignment.Bottom),
            NotifyPosition.BottomRight => (Avalonia.Layout.HorizontalAlignment.Right, Avalonia.Layout.VerticalAlignment.Bottom),
            NotifyPosition.TopCenter => (Avalonia.Layout.HorizontalAlignment.Center, Avalonia.Layout.VerticalAlignment.Top),
            _ => (Avalonia.Layout.HorizontalAlignment.Center, Avalonia.Layout.VerticalAlignment.Bottom)
        };

        Margin = new Avalonia.Thickness(12);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        NotificationHub.Instance.PositionChanged -= OnPositionChanged;
        NotificationHub.Instance.Notified -= OnNotified;
    }
}
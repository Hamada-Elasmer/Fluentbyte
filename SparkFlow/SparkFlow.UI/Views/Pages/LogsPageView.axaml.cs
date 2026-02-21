/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Views/Pages/LogsPageView.axaml.cs
 * Purpose: UI component: LogsPageView.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SparkFlow.UI.ViewModels.Pages;
using UtiliLib.Models;

namespace SparkFlow.UI.Views.Pages;

public partial class LogsPageView : UserControl
{
    private ListBox? _list;
    private LogsPageViewModel? _vm;

    public LogsPageView()
    {
        InitializeComponent();

        _list = this.FindControl<ListBox>("ConsoleList");

        DataContextChanged += (_, _) =>
        {
            if (_vm != null)
                _vm.RequestScrollToEnd -= Vm_RequestScrollToEnd;

            _vm = DataContext as LogsPageViewModel;

            if (_vm != null)
                _vm.RequestScrollToEnd += Vm_RequestScrollToEnd;
        };

        Unloaded += (_, _) =>
        {
            if (_vm != null)
                _vm.RequestScrollToEnd -= Vm_RequestScrollToEnd;
        };
    }

    private void Vm_RequestScrollToEnd(object? sender, LogEntry last)
        => _list?.ScrollIntoView(last);

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);
}
using System;
using System.Threading.Tasks;
using SparkFlow.Abstractions.Services.Dialogs;
using SparkFlow.Domain.Models.Dialogs;
using SparkFlow.UI.ViewModels.Dialogs.Contents;
using SparkFlow.UI.ViewModels.Shell;
using SparkFlow.UI.Views.Dialogs.Contents;
using SukiUI.Dialogs;

namespace SparkFlow.UI.Services.Dialogs;

public sealed class SukiDialogService : IDialogService
{
    private readonly MainWindowViewModel _mainWindow;

    public SukiDialogService(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
    }

    public Task<AppDialogResult> ShowAsync(AppDialogRequest request)
    {
        // Map core request to a message content dialog hosted by Suki
        var (primary, secondary, hasSecondary, headerTitle) = request.Kind switch
        {
            AppDialogKind.Confirm => (request.YesText ?? "Yes", request.NoText ?? "No", true, "Confirm"),
            AppDialogKind.Warning => ("OK", "", false, "Warning"),
            _ => ("OK", "", false, "Information")
        };

        return ShowMessageAsync(
            dialogTitle: string.IsNullOrWhiteSpace(request.Title) ? headerTitle : request.Title,
            contentTitle: request.Title ?? "",
            body: request.Body ?? "",
            primaryText: primary,
            secondaryText: secondary,
            hasSecondary: hasSecondary);
    }

    public Task<AppDialogResult> ShowWarningAsync(string title, string body) =>
        ShowAsync(new AppDialogRequest { Kind = AppDialogKind.Warning, Title = title, Body = body });

    public Task<AppDialogResult> ShowInfoAsync(string title, string body) =>
        ShowAsync(new AppDialogRequest { Kind = AppDialogKind.Info, Title = title, Body = body });

    public Task<AppDialogResult> ShowConfirmAsync(string title, string body, string yesText = "Yes", string noText = "No") =>
        ShowAsync(new AppDialogRequest { Kind = AppDialogKind.Confirm, Title = title, Body = body, YesText = yesText, NoText = noText });

    private Task<AppDialogResult> ShowMessageAsync(
        string dialogTitle,
        string contentTitle,
        string body,
        string primaryText,
        string secondaryText,
        bool hasSecondary)
    {
        var tcs = new TaskCompletionSource<AppDialogResult>();

        var vm = new SukiMessageDialogContentViewModel(
            title: contentTitle,
            body: body,
            primaryText: primaryText,
            secondaryText: secondaryText,
            hasSecondary: hasSecondary);

        var content = new SukiMessageDialogContent
        {
            DataContext = vm
        };

        vm.RequestClose += r => tcs.TrySetResult(r);

        // IMPORTANT:
        // We do NOT dismiss by clicking background for message dialogs,
        // because we need a deterministic AppDialogResult.
        _mainWindow.DialogManager.CreateDialog()
            .WithTitle(dialogTitle ?? "")
            .WithContent(content)
            .TryShow();

        return tcs.Task;
    }
}

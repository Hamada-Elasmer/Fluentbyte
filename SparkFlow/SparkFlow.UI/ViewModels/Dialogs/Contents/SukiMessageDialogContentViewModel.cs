using System;
using CommunityToolkit.Mvvm.Input;
using SparkFlow.Domain.Models.Dialogs;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Dialogs.Contents;

public sealed class SukiMessageDialogContentViewModel : ViewModelBase
{
    public string Title { get; }
    public string Body { get; }

    public string PrimaryText { get; }
    public string SecondaryText { get; }

    public bool HasSecondary { get; }

    public IRelayCommand PrimaryCommand { get; }
    public IRelayCommand SecondaryCommand { get; }

    public event Action<AppDialogResult>? RequestClose;

    public SukiMessageDialogContentViewModel(
        string title,
        string body,
        string primaryText,
        string secondaryText,
        bool hasSecondary)
    {
        Title = title ?? "";
        Body = body ?? "";

        PrimaryText = string.IsNullOrWhiteSpace(primaryText) ? "OK" : primaryText;
        SecondaryText = secondaryText ?? "";
        HasSecondary = hasSecondary;

        PrimaryCommand = new RelayCommand(() =>
            RequestClose?.Invoke(new AppDialogResult
            {
                IsAccepted = true,
                IsCanceled = false,
                Value = "primary"
            }));

        SecondaryCommand = new RelayCommand(() =>
            RequestClose?.Invoke(new AppDialogResult
            {
                IsAccepted = false,
                IsCanceled = true,
                Value = "secondary"
            }));
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Services/Dialogs/SukiAppDialogService.cs
 * Purpose: UI component: SukiAppDialogService.
 * Notes:
 *  - Implements IAppDialogService using SukiUI dialogs.
 *  - Delegates core dialog methods (Info/Warning/Confirm/ShowAsync) to SukiDialogService.
 *  - Hosts content dialogs (Health/Game) via typed factories.
 * ============================================================================ */

using System;
using System.Threading.Tasks;
using SparkFlow.Abstractions.Services.Dialogs;
using SparkFlow.Domain.Models.Dialogs;
using SukiUI.Dialogs;

namespace SparkFlow.UI.Services.Dialogs;

public sealed class SukiAppDialogService : IAppDialogService
{
    private readonly ISukiDialogManager _dm;
    private readonly IDialogService _coreDialogs;

    private readonly HealthDialogContentFactory _healthFactory;
    private readonly GameDialogContentFactory _gameFactory;

    public SukiAppDialogService(
        ISukiDialogManager dialogManager,
        IDialogService coreDialogs,
        HealthDialogContentFactory healthFactory,
        GameDialogContentFactory gameFactory)
    {
        _dm = dialogManager ?? throw new ArgumentNullException(nameof(dialogManager));
        _coreDialogs = coreDialogs ?? throw new ArgumentNullException(nameof(coreDialogs));
        _healthFactory = healthFactory ?? throw new ArgumentNullException(nameof(healthFactory));
        _gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
    }

    // =========================================================
    // Core dialogs (delegate to SukiDialogService)
    // =========================================================
    public Task<AppDialogResult> ShowAsync(AppDialogRequest request)
        => _coreDialogs.ShowAsync(request);

    public Task<AppDialogResult> ShowWarningAsync(string title, string body)
        => _coreDialogs.ShowWarningAsync(title, body);

    public Task<AppDialogResult> ShowInfoAsync(string title, string body)
        => _coreDialogs.ShowInfoAsync(title, body);

    public Task<AppDialogResult> ShowConfirmAsync(string title, string body, string yesText = "Yes", string noText = "No")
        => _coreDialogs.ShowConfirmAsync(title, body, yesText, noText);

    // =========================================================
    // Content dialogs (Suki Dialog Host)
    // =========================================================
    public Task<AppDialogResult> ShowAccountHealthCheckAsync(string profileId, string primaryText = "Close")
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return Task.FromResult(new AppDialogResult
            {
                IsAccepted = false,
                IsCanceled = true,
                Value = "invalid_profile"
            });
        }

        object content = _healthFactory(profileId);

        _dm.CreateDialog()
            .WithTitle("Health Check")
            .WithContent(content)
            .Dismiss().ByClickingBackground()
            .TryShow();

        return Task.FromResult(new AppDialogResult
        {
            IsAccepted = true,
            IsCanceled = false,
            Value = "shown"
        });
    }

    public Task<AppDialogResult> ShowGameInstallAsync(string profileId, string primaryText = "Close")
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return Task.FromResult(new AppDialogResult
            {
                IsAccepted = false,
                IsCanceled = true,
                Value = "invalid_profile"
            });
        }

        object content = _gameFactory(profileId);

        _dm.CreateDialog()
            .WithTitle("Game Info")
            .WithContent(content)
            .Dismiss().ByClickingBackground()
            .TryShow();

        return Task.FromResult(new AppDialogResult
        {
            IsAccepted = true,
            IsCanceled = false,
            Value = "shown"
        });
    }
}

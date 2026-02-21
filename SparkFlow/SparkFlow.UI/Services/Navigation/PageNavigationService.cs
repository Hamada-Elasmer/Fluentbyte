using System;
using Avalonia.Threading;
using SparkFlow.Domain.Models.Pages;

namespace SparkFlow.UI.Services.Navigation;

public sealed class PageNavigationService : IPageNavigationService
{
    public event Action<PagesEnum>? NavigationRequested;

    public void RequestNavigation(PagesEnum page)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            NavigationRequested?.Invoke(page);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            NavigationRequested?.Invoke(page);
        });
    }
}
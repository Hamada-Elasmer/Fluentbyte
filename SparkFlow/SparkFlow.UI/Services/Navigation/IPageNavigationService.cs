using System;
using SparkFlow.Domain.Models.Pages;

namespace SparkFlow.UI.Services.Navigation;

public interface IPageNavigationService
{
    event Action<PagesEnum>? NavigationRequested;

    void RequestNavigation(PagesEnum page);
}

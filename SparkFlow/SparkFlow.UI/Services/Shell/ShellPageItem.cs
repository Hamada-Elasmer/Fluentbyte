using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;
using SparkFlow.Domain.Models.Pages;
using SukiUI.Controls;

namespace SparkFlow.UI.Services.Shell;

public sealed class ShellSideMenuItem : SukiSideMenuItem
{
    public PagesEnum Page { get; }

    /// <summary>
    /// Creates a Suki side-menu item with optional (lazy) page content.
    /// If pageContent is null, it can be assigned later when navigation happens.
    /// </summary>
    public ShellSideMenuItem(
        string title,
        MaterialIconKind icon,
        PagesEnum page,
        UserControl? pageContent = null)
    {
        Page = page;

        Header = title;
        Classes.Add("Compact");

        Icon = new MaterialIcon
        {
            Kind = icon,
            Width = 18,
            Height = 18
        };

        // Required by SukiSideMenu
        if (pageContent != null) PageContent = pageContent;
    }
}
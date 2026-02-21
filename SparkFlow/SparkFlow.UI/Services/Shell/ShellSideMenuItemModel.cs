using Material.Icons;
using SparkFlow.Domain.Models.Pages;

namespace SparkFlow.UI.Services.Shell;

/// <summary>
/// ShellSideMenuItemModel
///
/// Lightweight sidebar navigation model for SukiSideMenu.
///
/// Notes:
/// - This is NOT a Control.
/// - It is pure data (Title + Icon + Page).
/// - Prevents Avalonia "visual parent" crashes caused by using Controls inside ItemsSource.
/// - Overrides ToString() to control what SukiSideMenu may show as "Selected Page Title".
/// </summary>
public sealed class ShellSideMenuItemModel
{
    // ==========================================================
    // Core Navigation Metadata
    // ==========================================================

    public PagesEnum Page { get; }

    // ==========================================================
    // Display Properties
    // ==========================================================

    public string Title { get; }

    public MaterialIconKind Icon { get; }

    // ==========================================================
    // Constructor
    // ==========================================================

    public ShellSideMenuItemModel(
        string title,
        MaterialIconKind icon,
        PagesEnum page)
    {
        Title = title;
        Icon = icon;
        Page = page;
    }

    // ==========================================================
    // Debug / UI Display Helper
    // ==========================================================
    //
    // SukiSideMenu may call ToString() internally to display
    // the selected page title above the menu list.
    //
    // We do NOT want that extra title row, so return empty.
    //

    public override string ToString()
        => string.Empty;
}

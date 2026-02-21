/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Pages/Home/FeatureCardViewModel.cs
 * Purpose: UI component: FeatureCardViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Pages.Home;

public sealed class FeatureCardViewModel : ViewModelBase
{
    // ===============================
    // Display
    // ===============================
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => RaiseAndSetIfChanged(ref _name, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => RaiseAndSetIfChanged(ref _description, value);
    }

    // ===============================
    // Badges
    // ===============================
    private bool _isNew;
    public bool IsNew
    {
        get => _isNew;
        set => RaiseAndSetIfChanged(ref _isNew, value);
    }

    private bool _isUpdated;
    public bool IsUpdated
    {
        get => _isUpdated;
        set => RaiseAndSetIfChanged(ref _isUpdated, value);
    }

    private bool _isSoon;
    public bool IsSoon
    {
        get => _isSoon;
        set => RaiseAndSetIfChanged(ref _isSoon, value);
    }

    // ===============================
    // Status
    // ===============================
    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    // ===============================
    // Optional Constructor (clean creation)
    // ===============================
    public FeatureCardViewModel(
        string name,
        string description,
        bool isEnabled = true,
        bool isNew = false,
        bool isUpdated = false,
        bool isSoon = false)
    {
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
        IsNew = isNew;
        IsUpdated = isUpdated;
        IsSoon = isSoon;
    }

    // Parameterless constructor still allowed for XAML / tooling
    public FeatureCardViewModel() { }
}
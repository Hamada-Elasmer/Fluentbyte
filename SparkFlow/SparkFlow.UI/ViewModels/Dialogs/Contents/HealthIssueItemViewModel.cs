/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/ViewModels/Dialogs/Contents/HealthIssueItemViewModel.cs
 * Purpose: UI component: HealthIssueItemViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Dialogs.Contents;

public sealed class HealthIssueItemViewModel : ViewModelBase
{
    public HealthIssue Model { get; }

    public string Title => Model.Title ?? "";
    public string Details => Model.Details ?? "";
    public HealthIssueSeverity Severity => Model.Severity;
    public HealthFixKind FixKind => Model.FixKind;

    public bool IsAutoFix => FixKind == HealthFixKind.Auto;
    public bool IsManualFix => FixKind == HealthFixKind.Manual;

    public bool CanFix => FixKind is HealthFixKind.Auto or HealthFixKind.Manual;

    public string ManualSteps => Model.ManualSteps ?? "";

    public HealthIssueItemViewModel(HealthIssue model)
    {
        Model = model;
    }
}
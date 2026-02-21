/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Shell/AppInfoViewModel.cs
 * Purpose: UI component: AppInfoViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.UI.ViewModels.Shell;

public class AppInfoViewModel : ViewModelBase
{
    private readonly IAppInfoService _appInfo;

    // ===============================
    // Computed (Global App Info)
    // ===============================
    public string AppTitle => _appInfo.AppTitle;
    public string Version  => _appInfo.Version;

    public string FooterText =>
        $"© {DateTime.Now.Year} {_appInfo.CompanyTitle} • All Rights Reserved";

    // ===============================
    // Constructor
    // ===============================
    public AppInfoViewModel(IAppInfoService appInfo)
    {
        _appInfo = appInfo;
    }
}
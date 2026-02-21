/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Settings/AppInfoService.cs
 * Purpose: Core component: AppInfoService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Abstractions;

public sealed class AppInfoService : IAppInfoService
{
    private readonly ISettingsService _settings;

    public AppInfoService(ISettingsService settings)
    {
        _settings = settings;
    }

    public string AppTitle =>
        _settings.Settings.AppTitle;

    public string Version =>
        _settings.Settings.Version;

    public string CompanyTitle =>
        _settings.Settings.CompanyTitle;
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Services/Windows/IAccountWindowsService.cs
 * Purpose: UI component: IAccountWindowsService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.UI.Services.Windows;

public interface IAccountWindowsService
{
    void OpenHealthCheck(string profileId);
    void OpenTasks(string profileId);
    void OpenDashboard(string profileId);
    void OpenGameInstall(string profileId);
}
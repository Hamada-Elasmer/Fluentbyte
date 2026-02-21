/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Abstractions/IAppInfoService.cs
 * Purpose: Core component: IAppInfoService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Abstractions
{
    public interface IAppInfoService
    {
        string AppTitle { get; }
        string Version { get; }
        string CompanyTitle { get; }
    }
}
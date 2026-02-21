/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Abstractions/IAppShutdownService.cs
 * Purpose: Core component: IAppShutdownService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.Abstractions;

public interface IAppShutdownService
{
    Task ShutdownAsync(CancellationToken ct = default);
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Storage/IHealthReportStore.cs
 * Purpose: Core component: IHealthReportStore.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;

namespace SparkFlow.Infrastructure.Services.Health.Storage;

public interface IHealthReportStore
{
    Task<HealthReport?> LoadLastAsync(string profileId, CancellationToken ct = default);
    Task SaveLastAsync(HealthReport report, CancellationToken ct = default);
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Interfaces/ISettingsProviderAsync.cs
 * Purpose: Library component: ISettingsProviderAsync.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Models;

namespace SettingsStore.Interfaces;

public interface ISettingsProviderAsync : ISettingsProvider
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
    Task<AppSettings> ResetAsync(CancellationToken ct = default);
}
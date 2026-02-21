/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Interfaces/ISettingsAccessorAsync.cs
 * Purpose: Library component: ISettingsAccessorAsync.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Models;

namespace SettingsStore.Interfaces;

public interface ISettingsAccessorAsync : ISettingsAccessor
{
    Task SaveAsync(CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
}
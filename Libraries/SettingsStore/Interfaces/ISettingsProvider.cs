/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Interfaces/ISettingsProvider.cs
 * Purpose: Library component: ISettingsProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Models;

namespace SettingsStore.Interfaces
{
    /// <summary>
    /// Low-level settings persistence abstraction.
    /// </summary>
    public interface ISettingsProvider
    {
        AppSettings Load();
        void Save(AppSettings settings);
        AppSettings Reset();
    }
}
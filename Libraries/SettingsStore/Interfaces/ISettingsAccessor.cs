/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Interfaces/ISettingsAccessor.cs
 * Purpose: Library component: ISettingsAccessor.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Models;

namespace SettingsStore.Interfaces
{
    /// <summary>
    /// High-level access to application settings.
    /// Used by Core.
    /// </summary>
    public interface ISettingsAccessor
    {
        AppSettings Current { get; }

        void Save();
        void Reset();
    }
}
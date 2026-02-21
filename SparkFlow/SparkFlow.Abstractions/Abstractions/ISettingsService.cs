/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.AbstractionsAbstractions/ISettingsService.cs
 * Purpose: Core component: ISettingsService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Models;

namespace SparkFlow.Abstractions.Abstractions
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        void Save();
        void Reset();
    }
}
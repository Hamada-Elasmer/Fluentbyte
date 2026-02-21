/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Settings/SettingsService.cs
 * Purpose: Core component: SettingsService.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Interfaces;
using SettingsStore.Models;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Settings
{
    public sealed class SettingsService(ISettingsAccessor accessor) : ISettingsService
    {
        public AppSettings Settings => accessor.Current;

        public void Save() => accessor.Save();

        public void Reset() => accessor.Reset();
    }
}
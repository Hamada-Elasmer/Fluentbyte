/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/SettingsStore/Providers/MemorySettingsProvider.cs
 * Purpose: Library component: MemorySettingsProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SettingsStore.Interfaces;
using SettingsStore.Models;

namespace SettingsStore.Providers;

/// <summary>
/// In-memory settings provider.
/// Useful for testing.
/// </summary>
public sealed class MemorySettingsProvider : ISettingsProviderAsync
{
    private AppSettings _settings = new();

    public AppSettings Load() => _settings;

    public Task<AppSettings> LoadAsync(CancellationToken ct = default) => Task.FromResult(_settings);

    public void Save(AppSettings settings) => _settings = settings;

    public Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        _settings = settings;
        return Task.CompletedTask;
    }

    public AppSettings Reset()
    {
        _settings = new AppSettings();
        return _settings;
    }

    public Task<AppSettings> ResetAsync(CancellationToken ct = default)
    {
        _settings = new AppSettings();
        return Task.FromResult(_settings);
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Accounts/AccountsSelector.cs
 * ============================================================================ */

using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Domain.Models.Accounts;
using UtiliLib;

namespace SparkFlow.Infrastructure.Services.Accounts;

public sealed class AccountsSelector : IAccountsSelector
{
    private readonly ISettingsService _settings;
    private readonly IProfilesStore _store;
    private readonly MLogger _log;

    public string SelectedProfileId => _settings.Settings.SelectedProfile;

    public AccountsSelector(ISettingsService settings, IProfilesStore store)
    {
        _settings = settings;
        _store = store;
        _log = MLogger.Instance;
    }

    public async Task<IReadOnlyList<AccountProfile>> GetEnabledOrderedAsync(CancellationToken ct = default)
    {
        var all = await _store.LoadAllAsync(ct);

        var enabled = all
            .Where(p => p.Active)
            .Where(p => !string.IsNullOrWhiteSpace(p.InstanceId))
            .OrderBy(p => p.CreatedAt ?? DateTimeOffset.MinValue)
            .ThenBy(p => p.Name)
            .ToList();

        return enabled;
    }

    public void Select(string? profileId)
    {
        var id = profileId?.Trim() ?? string.Empty;
        _settings.Settings.SelectedProfile = id;
        _settings.Save();
    }
}
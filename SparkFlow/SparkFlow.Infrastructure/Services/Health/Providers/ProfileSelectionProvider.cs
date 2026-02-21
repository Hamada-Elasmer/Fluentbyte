/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Health/Providers/ProfileSelectionProvider.cs
 * Purpose: Core component: ProfileSelectionProvider.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Models;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Health.Abstractions;

namespace SparkFlow.Infrastructure.Services.Health.Providers;

/// <summary>
/// Ensures selected profile is valid and active.
/// This uses only existing APIs (ProfilesStore + AccountsSelector).
/// </summary>
public sealed class ProfileSelectionProvider : IHealthCheckProvider
{
    private readonly IAccountsSelector _selector;
    private readonly IProfilesStore _store;

    public string Name => "ProfileSelection";

    public ProfileSelectionProvider(IAccountsSelector selector, IProfilesStore store)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<IReadOnlyList<HealthIssue>> CheckAsync(string profileId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var issues = new List<HealthIssue>();

        // If user opened health check for a profile that is not the selected one, that is OK.
        // But we still validate the selected profile state for consistency.
        var selected = _selector.SelectedProfileId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(selected))
        {
            issues.Add(new HealthIssue
            {
                Code = "profile.selected.empty",
                Title = "No selected profile",
                Details = "SelectedProfile is empty. Some pages may not know which profile to operate on.",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.Manual,
                ManualSteps = "Open Accounts page and select a profile."
            });
            return issues;
        }

        var p = await _store.GetByIdAsync(selected, ct);
        if (p is null)
        {
            issues.Add(new HealthIssue
            {
                Code = "profile.selected.missing",
                Title = "Selected profile does not exist",
                Details = "SelectedProfile points to a profile file that is missing in runtime/profiles.",
                Severity = HealthIssueSeverity.Blocker,
                FixKind = HealthFixKind.Manual,
                ManualSteps = "Open Accounts page and select a valid profile."
            });
            return issues;
        }

        if (!p.Active)
        {
            issues.Add(new HealthIssue
            {
                Code = "profile.selected.disabled",
                Title = "Selected profile is disabled",
                Details = "The selected profile exists but is not Active/Enabled.",
                Severity = HealthIssueSeverity.Warning,
                FixKind = HealthFixKind.Manual,
                ManualSteps = "Enable the profile from Accounts page."
            });
        }

        return issues;
    }

    public Task<int> FixAllAutoAsync(string profileId, CancellationToken ct = default)
    {
        // No safe auto-fix for selection. User must choose.
        return Task.FromResult(0);
    }
}
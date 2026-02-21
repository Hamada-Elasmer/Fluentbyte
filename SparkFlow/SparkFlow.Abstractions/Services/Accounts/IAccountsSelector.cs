/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Accounts/IAccountsSelector.cs
 * Purpose: Core component: IAccountsSelector.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Abstractions.Services.Accounts;

public interface IAccountsSelector
{
    string SelectedProfileId { get; }
    void Select(string? profileId);

    Task<IReadOnlyList<AccountProfile>> GetEnabledOrderedAsync(CancellationToken ct = default);
}
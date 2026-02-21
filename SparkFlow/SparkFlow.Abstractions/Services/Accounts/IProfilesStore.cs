/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Accounts/IProfilesStore.cs
 * Purpose: Core component: IProfilesStore.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Abstractions.Services.Accounts;

public interface IProfilesStore
{
    /// <summary>
    /// Fired when a profile is saved or deleted.
    /// The argument is the profileId (string).
    /// </summary>
    event Action<string>? ProfileChanged;

    Task<IReadOnlyList<AccountProfile>> LoadAllAsync(CancellationToken ct = default);
    Task<AccountProfile?> GetByIdAsync(string id, CancellationToken ct = default);
    Task SaveAsync(AccountProfile profile, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
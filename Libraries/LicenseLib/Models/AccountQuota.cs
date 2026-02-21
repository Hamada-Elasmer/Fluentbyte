/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Models/AccountQuota.cs
 * Purpose: Library component: AccountQuota.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace LicenseLib.Models;

public class AccountQuota
{
    public int CurrentAccountsUsed { get; set; } = 0;
    public int MaxAccountsAllowed { get; set; } = 1;
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Models/LicenseLimits.cs
 * Purpose: Library component: LicenseLimits.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace LicenseLib.Models
{
    public class LicenseLimits
    {
        public int MaxAccounts { get; set; } = 1;
        public DateTime ExpiryDate { get; set; } = DateTime.MaxValue;
        public bool MultiAccountEnabled { get; set; } = false;
        public bool MultiGameEnabled { get; set; } = false;
    }
}
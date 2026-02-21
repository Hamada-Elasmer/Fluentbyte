/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Interfaces/ILicenseValidator.cs
 * Purpose: Library component: ILicenseValidator.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using LicenseLib.Models;

namespace LicenseLib.Interfaces;

public interface ILicenseValidator
{
    /// <summary>
    /// Legacy validation API (kept for backward compatibility).
    /// Returns true if the license exists, is not expired, and passes security rules (if configured).
    /// </summary>
    bool ValidateLicense(string licenseKey);

    /// <summary>
    /// Returns limits (payload) for a given license key. If missing, returns an empty LicenseLimits instance.
    /// </summary>
    LicenseLimits GetLicenseLimits(string licenseKey);

    /// <summary>
    /// Rich validation result that includes failure reason and message.
    /// </summary>
    LicenseValidationResult Validate(string licenseKey);
}
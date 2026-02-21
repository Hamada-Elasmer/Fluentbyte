/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Models/LicenseValidationResult.cs
 * Purpose: Library component: LicenseValidationResult.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace LicenseLib.Models;

public sealed record LicenseValidationResult(
    bool IsValid,
    LicenseValidationFailureReason Reason,
    string Message
)
{
    public static LicenseValidationResult Ok() =>
        new(true, LicenseValidationFailureReason.None, "OK");

    public static LicenseValidationResult Fail(LicenseValidationFailureReason reason, string message) =>
        new(false, reason, message);
}
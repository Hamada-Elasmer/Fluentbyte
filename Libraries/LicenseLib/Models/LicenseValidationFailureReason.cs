/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Models/LicenseValidationFailureReason.cs
 * Purpose: Library component: LicenseValidationFailureReason.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace LicenseLib.Models;

public enum LicenseValidationFailureReason
{
    None = 0,
    Missing = 1,
    Expired = 2,
    SignatureMissing = 3,
    SignatureInvalid = 4,
    CorruptStore = 5
}
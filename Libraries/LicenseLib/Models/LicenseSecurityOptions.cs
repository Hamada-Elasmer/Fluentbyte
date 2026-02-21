/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Models/LicenseSecurityOptions.cs
 * Purpose: Library component: LicenseSecurityOptions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace LicenseLib.Models;

public sealed class LicenseSecurityOptions
{
    /// <summary>
    /// PEM public key used to verify signatures. If null/empty, signature verification is skipped.
    /// </summary>
    public string? PublicKeyPem { get; init; }

    /// <summary>
    /// If true, an entry must include a signature and it must verify successfully.
    /// Only enforced when PublicKeyPem is provided.
    /// </summary>
    public bool RequireSignature { get; init; } = true;

    /// <summary>
    /// When true, validation uses UTC time (recommended).
    /// </summary>
    public bool UseUtcNow { get; init; } = true;
}
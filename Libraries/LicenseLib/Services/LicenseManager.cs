/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Services/LicenseManager.cs
 * Purpose: Library component: LicenseManager.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using LicenseLib.Abstractions;
using LicenseLib.Interfaces;
using LicenseLib.Internal;
using LicenseLib.Models;
using LicenseLib.Providers;

namespace LicenseLib.Services;

public sealed class LicenseManager : ILicenseValidator
{
    private readonly LicenseFileProvider _provider;
    private readonly ISystemClock _clock;
    private readonly LicenseSecurityOptions _security;

    private Dictionary<string, LicenseEnvelope> _licenses;

    public LicenseManager(
        LicenseFileProvider provider,
        LicenseSecurityOptions? securityOptions = null,
        ISystemClock? clock = null)
    {
        _provider = provider;
        _security = securityOptions ?? new LicenseSecurityOptions();
        _clock = clock ?? new SystemClock();

        _licenses = _provider.LoadEnvelopes();
    }

    /// <summary>
    /// Reloads licenses from storage (useful if file can change while app is running).
    /// </summary>
    public void Reload()
    {
        _licenses = _provider.LoadEnvelopes();
    }

    /// <summary>
    /// Legacy validation API (kept for backward compatibility).
    /// </summary>
    public bool ValidateLicense(string licenseKey) => Validate(licenseKey).IsValid;

    public LicenseValidationResult Validate(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return LicenseValidationResult.Fail(LicenseValidationFailureReason.Missing, "License key is empty.");

        if (_licenses == null || _licenses.Count == 0)
            return LicenseValidationResult.Fail(LicenseValidationFailureReason.CorruptStore, "License store is empty or could not be loaded.");

        if (!_licenses.TryGetValue(licenseKey, out var env))
            return LicenseValidationResult.Fail(LicenseValidationFailureReason.Missing, "License key not found.");

        var now = _security.UseUtcNow ? _clock.UtcNow : _clock.Now;

        if (env.Payload.ExpiryDate < now)
            return LicenseValidationResult.Fail(LicenseValidationFailureReason.Expired, "License is expired.");

        // Signature verification (optional but recommended)
        if (!string.IsNullOrWhiteSpace(_security.PublicKeyPem))
        {
            if (_security.RequireSignature && string.IsNullOrWhiteSpace(env.Signature))
                return LicenseValidationResult.Fail(LicenseValidationFailureReason.SignatureMissing, "Signature is missing.");

            if (!string.IsNullOrWhiteSpace(env.Signature))
            {
                var canonical = CanonicalJson.SerializeCanonical(env.Payload);
                var ok = RsaSignatureVerifier.Verify(_security.PublicKeyPem!, canonical, env.Signature!);
                if (!ok)
                    return LicenseValidationResult.Fail(LicenseValidationFailureReason.SignatureInvalid, "Signature verification failed.");
            }
        }

        return LicenseValidationResult.Ok();
    }

    public LicenseLimits GetLicenseLimits(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return new LicenseLimits();

        return _licenses.TryGetValue(licenseKey, out var env)
            ? env.Payload
            : new LicenseLimits();
    }

    public void SaveChanges()
    {
        _provider.SaveEnvelopes(_licenses);
    }
}
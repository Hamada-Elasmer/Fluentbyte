/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Internal/RsaSignatureVerifier.cs
 * Purpose: Library component: RsaSignatureVerifier.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Security.Cryptography;
using System.Text;

namespace LicenseLib.Internal;

internal static class RsaSignatureVerifier
{
    public static bool Verify(string publicKeyPem, string data, string signatureBase64)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem)) return false;
        if (string.IsNullOrWhiteSpace(signatureBase64)) return false;

        byte[] signature;
        try { signature = Convert.FromBase64String(signatureBase64); }
        catch { return false; }

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var bytes = Encoding.UTF8.GetBytes(data);
        return rsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
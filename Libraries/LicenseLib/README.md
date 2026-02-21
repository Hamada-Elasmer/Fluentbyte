# LicenseLib

**LicenseLib** is a .NET library for license storage and validation inside applications.  
It supports JSON-based storage, expiry checks, and optional **RSA signature verification** for tamper resistance.

---

## Goals

- Validate a license key exists
- Validate the license is not expired
- Return license limits (quotas / features) for feature gating
- Store licenses in a simple JSON file
- (Optional) Verify license integrity using RSA signatures

---

## Main Components

```
LicenseLib/
│
├── Interfaces/
│   └── ILicenseValidator.cs
│
├── Providers/
│   └── LicenseFileProvider.cs
│
├── Services/
│   ├── LicenseManager.cs
│   └── SystemClock.cs
│
├── Abstractions/
│   └── ISystemClock.cs
│
├── Models/
│   ├── LicenseLimits.cs
│   ├── AccountQuota.cs
│   ├── LicenseEnvelope.cs
│   ├── LicenseSecurityOptions.cs
│   ├── LicenseValidationResult.cs
│   └── LicenseValidationFailureReason.cs
│
└── Internal/
    ├── CanonicalJson.cs
    └── RsaSignatureVerifier.cs
```

---

## Quick Start

### 1) Create a provider and manager

```csharp
using LicenseLib.Models;
using LicenseLib.Providers;
using LicenseLib.Services;

var provider = new LicenseFileProvider("Data/license.json");

// If you want RSA signature validation:
var security = new LicenseSecurityOptions
{
    PublicKeyPem = File.ReadAllText("public_key.pem"),
    RequireSignature = true,
    UseUtcNow = true
};

var manager = new LicenseManager(provider, security);
```

### 2) Validate (legacy API)

```csharp
bool ok = manager.ValidateLicense("ABC-123");
```

### 3) Validate with a rich result (recommended)

```csharp
var result = manager.Validate("ABC-123");
if (!result.IsValid)
{
    Console.WriteLine($"{result.Reason}: {result.Message}");
}
```

### 4) Read license limits

```csharp
var limits = manager.GetLicenseLimits("ABC-123");
```

---

## License File Formats

### Envelope format (recommended)

Supports payload + signature:

```json
{
  "ABC-123": {
    "payload": {
      "expiryDate": "2030-01-01T00:00:00Z",
      "maxAccounts": 10
    },
    "signature": "BASE64_SIGNATURE",
    "kid": "main"
  }
}
```

### Legacy format (supported)

No envelope/signature:

```json
{
  "ABC-123": {
    "expiryDate": "2030-01-01T00:00:00Z",
    "maxAccounts": 10
  }
}
```

---

## Notes

- RSA validation is optional, but recommended for production licenses.
- Keep private keys out of the client application.

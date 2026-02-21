/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Accounts/ProfileDeviceBinder.cs
 * Purpose: Core service: initial binding & repair (Profile ↔ Device) using a known serial.
 * Notes:
 *  - Core owns all binding decisions.
 *  - UI may request binding by providing a serial, but never reads identity keys directly.
 * ============================================================================ */

using AdbLib.Abstractions;
using DeviceBindingLib.Abstractions;
using SparkFlow.Domain.Models.Accounts;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Accounts;

public sealed class ProfileDeviceBinder
{
    private readonly IAdbClient _adb;
    private readonly IDeviceIdentityService _identity;

    public ProfileDeviceBinder(IAdbClient adb, IDeviceIdentityService identity)
    {
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }

    /// <summary>
    /// Best-effort bind for a profile to a specific device serial.
    /// Creates GUID if missing and stores AndroidId as fallback.
    /// Never throws.
    /// </summary>
    public void BindBestEffort(AccountProfile profile, string adbSerial)
    {
        if (profile is null)
            return;

        profile.Binding ??= new DeviceBindingData();

        var serial = adbSerial?.Trim();

        if (string.IsNullOrWhiteSpace(serial))
        {
            MLogger.Instance.Warn(LogChannel.SYSTEM,
                $"[Binding] BindBestEffort skipped: adbSerial is empty (ProfileId='{profile.Id}').");
            return;
        }

        // ✅ ALWAYS persist serial FIRST (even if identity fails later)
        try
        {
            profile.AdbSerial = serial;
            profile.Binding.LastKnownAdbSerial = serial;
        }
        catch
        {
            // ignored
        }

        // ✅ AndroidId (best-effort)
        try
        {
            var androidId = _identity.ReadAndroidId(serial);
            if (!string.IsNullOrWhiteSpace(androidId))
                profile.Binding.AndroidId = androidId;
        }
        catch (Exception ex)
        {
            MLogger.Instance.Warn(LogChannel.SYSTEM,
                $"[Binding] ReadAndroidId failed for {serial}: {ex.Message}");
        }

        // ✅ GUID (best-effort)
        try
        {
            var guid = _identity.GetOrCreateGuid(serial);

            if (!string.IsNullOrWhiteSpace(guid.Guid))
                profile.Binding.BoundGuid = guid.Guid;

            MLogger.Instance.Info(LogChannel.SYSTEM,
                $"[Binding] Bound profile '{profile.Id}' to {serial} | GuidCreated={guid.WasCreated} | AndroidId={(profile.Binding.AndroidId ?? "null")}");
        }
        catch (Exception ex)
        {
            MLogger.Instance.Warn(LogChannel.SYSTEM,
                $"[Binding] GetOrCreateGuid failed for {serial}: {ex.Message}");
        }
    }
}

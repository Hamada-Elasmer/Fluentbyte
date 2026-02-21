using System;
using AdbLib.Abstractions;
using DeviceBindingLib.Abstractions;
using DeviceBindingLib.Internal;
using DeviceBindingLib.Models;
using UtiliLib;
using UtiliLib.Types;

namespace DeviceBindingLib.Services;

/// <summary>
/// Production device identity service.
/// Uses SparkFlow-owned GUID (primary) + AndroidId (fallback).
/// </summary>
public sealed class DeviceIdentityService : IDeviceIdentityService
{
    private readonly IAdbClient _adb;

    public DeviceIdentityService(IAdbClient adb)
    {
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
    }

    public DeviceIdentityResult GetOrCreateGuid(string adbSerial)
    {
        var existing = _adb.Shell(adbSerial, $"cat {DeviceIdPaths.GuidFilePath}");

        if (!string.IsNullOrWhiteSpace(existing))
        {
            return new DeviceIdentityResult
            {
                Guid = existing.Trim(),
                WasCreated = false
            };
        }

        var guid = Guid.NewGuid().ToString("N");

        _adb.Shell(adbSerial, $"echo {guid} > {DeviceIdPaths.GuidFilePath}");

        MLogger.Instance.Info(
            LogChannel.SYSTEM,
            $"[Binding] SparkFlow GUID created | Serial={adbSerial} | Guid={guid}");

        return new DeviceIdentityResult
        {
            Guid = guid,
            WasCreated = true
        };
    }

    public string? ReadAndroidId(string adbSerial)
    {
        var id = _adb.Shell(adbSerial, "settings get secure android_id");
        return string.IsNullOrWhiteSpace(id) ? null : id.Trim();
    }
}

using System.Collections.Generic;
using DeviceBindingLib.Abstractions;
using DeviceBindingLib.Models;

namespace DeviceBindingLib.Services;

/// <summary>
/// Matches devices using GUID first, then AndroidId fallback.
/// Also supports LastKnownAdbSerial as a final, safe online hint.
/// </summary>
public sealed class DeviceBindingResolver : IDeviceBindingResolver
{
    private readonly IDeviceIdentityService _identity;

    public DeviceBindingResolver(IDeviceIdentityService identity)
    {
        _identity = identity;
    }

    public DeviceResolveResult Resolve(DeviceBindingInfo binding, IReadOnlyList<string> onlineSerials)
    {
        // If we have a last-known serial and it is online, trust it as a safe hint
        if (!string.IsNullOrWhiteSpace(binding.LastKnownAdbSerial))
        {
            var last = binding.LastKnownAdbSerial.Trim();
            foreach (var s in onlineSerials)
            {
                if (string.Equals(s, last, System.StringComparison.OrdinalIgnoreCase))
                    return DeviceResolveResult.Online(s);
            }
        }

        if (string.IsNullOrWhiteSpace(binding.BoundGuid) &&
            string.IsNullOrWhiteSpace(binding.AndroidId))
            return DeviceResolveResult.Unbound();

        foreach (var serial in onlineSerials)
        {
            var guid = _identity.GetOrCreateGuid(serial);

            if (!string.IsNullOrWhiteSpace(binding.BoundGuid) &&
                guid.Guid == binding.BoundGuid)
                return DeviceResolveResult.Online(serial);

            var androidId = _identity.ReadAndroidId(serial);

            if (!string.IsNullOrWhiteSpace(binding.AndroidId) &&
                androidId == binding.AndroidId)
            {
                // Auto-repair GUID
                binding.BoundGuid = guid.Guid;
                binding.LastKnownAdbSerial = serial;
                return DeviceResolveResult.Online(serial);
            }
        }

        return DeviceResolveResult.Missing();
    }
}

/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Accounts/ProfileDeviceResolver.cs
 * Purpose: Core service: runtime resolver (Profile → Online ADB serial).
 * Notes:
 *  - GUID is primary binding key.
 *  - AndroidId is fallback.
 *  - Auto-Repair: if GUID missing but AndroidId matches, GUID is recreated.
 * ============================================================================ */

using AdbLib.Abstractions;
using DeviceBindingLib.Abstractions;
using DeviceBindingLib.Models;
using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Infrastructure.Services.Accounts;

public sealed class ProfileDeviceResolver
{
    private readonly IAdbClient _adb;
    private readonly IDeviceBindingResolver _resolver;

    public ProfileDeviceResolver(IAdbClient adb, IDeviceBindingResolver resolver)
    {
        _adb = adb;
        _resolver = resolver;
    }

    public DeviceResolveResult Resolve(AccountProfile profile)
    {
        var onlineSerials = GetOnlineSerialsSafe();

        var binding = new DeviceBindingInfo
        {
            BoundGuid = profile.Binding?.BoundGuid,
            AndroidId = profile.Binding?.AndroidId,
            LastKnownAdbSerial = profile.Binding?.LastKnownAdbSerial,
            InstanceHintId = NormalizeInstanceId(profile.InstanceId)
        };

        var result = _resolver.Resolve(binding, onlineSerials);

        // Persist Auto-Repair results back into profile model (in-memory).
        profile.Binding ??= new DeviceBindingData();
        profile.Binding.BoundGuid = binding.BoundGuid;
        profile.Binding.LastKnownAdbSerial = binding.LastKnownAdbSerial;

        // Keep AdbSerial updated for legacy paths
        if (!string.IsNullOrWhiteSpace(result.ResolvedSerial))
            profile.AdbSerial = result.ResolvedSerial;

        return result;
    }

    private static string? NormalizeInstanceId(string? instanceId)
    {
        instanceId = string.IsNullOrWhiteSpace(instanceId) ? null : instanceId.Trim();
        if (instanceId == "-1") return null;
        return instanceId;
    }

    private List<string> GetOnlineSerialsSafe()
    {
        try
        {
            var devices = _adb.Devices();
            var serials = new List<string>();

            foreach (var d in devices)
            {
                if (string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase))
                    serials.Add(d.Serial);
            }

            return serials;
        }
        catch
        {
            return new List<string>();
        }
    }
}

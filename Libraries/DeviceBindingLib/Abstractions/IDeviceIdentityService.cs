using DeviceBindingLib.Models;

namespace DeviceBindingLib.Abstractions;

public interface IDeviceIdentityService
{
    DeviceIdentityResult GetOrCreateGuid(string adbSerial);
    string? ReadAndroidId(string adbSerial);
}

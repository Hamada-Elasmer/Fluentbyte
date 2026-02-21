using DeviceBindingLib.Models;

namespace DeviceBindingLib.Abstractions;

public interface IDeviceBindingResolver
{
    DeviceResolveResult Resolve(DeviceBindingInfo binding, IReadOnlyList<string> onlineSerials);
}

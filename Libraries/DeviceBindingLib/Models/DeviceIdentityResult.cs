namespace DeviceBindingLib.Models;

public sealed class DeviceIdentityResult
{
    public string Guid { get; init; } = string.Empty;
    public bool WasCreated { get; init; }
}

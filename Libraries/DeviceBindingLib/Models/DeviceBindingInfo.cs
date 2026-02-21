namespace DeviceBindingLib.Models;

public sealed class DeviceBindingInfo
{
    public string? BoundGuid { get; set; }
    public string? AndroidId { get; set; }
    public string? LastKnownAdbSerial { get; set; }

    // Best-effort hint only (not required by resolver).
    public string? InstanceHintId { get; set; }
}

namespace DeviceBindingLib.Models;

public sealed class DeviceResolveResult
{
    public string? ResolvedSerial { get; init; }
    public BindingState State { get; init; }

    public static DeviceResolveResult Online(string serial)
        => new() { ResolvedSerial = serial, State = BindingState.BoundOnline };

    public static DeviceResolveResult Missing()
        => new() { State = BindingState.Missing };

    public static DeviceResolveResult Unbound()
        => new() { State = BindingState.Unbound };
}

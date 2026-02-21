namespace EmulatorLib.Abstractions;

public interface IEmulator
{
    bool IsInstalled { get; }

    IReadOnlyList<IEmulatorInstance> ScanInstances();

    IEmulatorInstance CreateInstanceFromTemplate(
        string templateInstanceId,
        string newName);

    IEmulatorInstance? TryGetInstance(string instanceId);
}

namespace EmulatorLib.Abstractions;

public interface IEmulatorInstance : IAdbPortProvider
{
    string InstanceId { get; }
    string Name { get; }

    EmulatorState State { get; }

    void Start();
    void Stop();

    Task WaitUntilOnlineAsync(CancellationToken ct);
}

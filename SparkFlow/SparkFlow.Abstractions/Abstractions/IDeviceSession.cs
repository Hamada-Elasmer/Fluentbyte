namespace SparkFlow.Abstractions.Abstractions;

/// <summary>
/// Represents a single logical device session bound to an ADB serial.
/// Wraps the existing IAdbClient APIs (sync + async mix) in a safe session abstraction.
/// </summary>
public interface IDeviceSession : IAsyncDisposable
{
    string AdbSerial { get; }

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);

    Task<string> ShellAsync(
        string command,
        int timeoutMs = 12_000,
        CancellationToken cancellationToken = default);

    Task StartActivityAsync(
        string component,
        int timeoutMs = 20_000,
        CancellationToken cancellationToken = default);

    Task<bool> IsPackageRunningAsync(
        string packageName,
        int timeoutMs = 8_000,
        CancellationToken cancellationToken = default);

    Task ForceStopAsync(
        string packageName,
        int timeoutMs = 8_000,
        CancellationToken cancellationToken = default);
}

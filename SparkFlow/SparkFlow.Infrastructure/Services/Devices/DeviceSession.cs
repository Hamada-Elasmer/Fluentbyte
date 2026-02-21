using AdbLib.Abstractions;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Devices;

/// <summary>
/// Default implementation of IDeviceSession.
/// Wraps IAdbClient.
/// </summary>
internal sealed class DeviceSession : IDeviceSession
{
    private readonly IAdbClient _adb;
    private bool _disposed;

    public DeviceSession(string adbSerial, IAdbClient adbClient)
    {
        AdbSerial = adbSerial ?? throw new ArgumentNullException(nameof(adbSerial));
        _adb = adbClient ?? throw new ArgumentNullException(nameof(adbClient));
    }

    public string AdbSerial { get; }

    public async Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _adb.WaitForDeviceReadyAsync(AdbSerial, cancellationToken).ConfigureAwait(false);
    }

    public Task<string> ShellAsync(
        string command,
        int timeoutMs = 12_000,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty.", nameof(command));

        return _adb.ShellAsync(AdbSerial, command, timeoutMs, cancellationToken);
    }

    public Task StartActivityAsync(
        string component,
        int timeoutMs = 20_000,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(component))
            throw new ArgumentException(nameof(component));

        return _adb.StartActivityAsync(AdbSerial, component, timeoutMs, cancellationToken);
    }

    public Task<bool> IsPackageRunningAsync(
        string packageName,
        int timeoutMs = 8_000,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException(nameof(packageName));

        return _adb.IsPackageRunningAsync(AdbSerial, packageName, timeoutMs, cancellationToken);
    }

    public Task ForceStopAsync(
        string packageName,
        int timeoutMs = 8_000,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(packageName))
            throw new ArgumentException(nameof(packageName));

        return _adb.ForceStopPackageAsync(AdbSerial, packageName, timeoutMs, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DeviceSession));
    }
}

using System.Collections.Concurrent;
using AdbLib.Abstractions;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Devices
{
    /// <summary>
    /// Default implementation of IDeviceSessionFactory.
    /// Ensures exclusive access to a device based on its ADB serial.
    /// </summary>
    public sealed class DeviceSessionFactory : IDeviceSessionFactory
    {
        private readonly IAdbClient _adbClient;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceLocks
            = new(StringComparer.OrdinalIgnoreCase);

        public DeviceSessionFactory(IAdbClient adbClient)
        {
            _adbClient = adbClient ?? throw new ArgumentNullException(nameof(adbClient));
        }

        public async Task<IDeviceSession> OpenSessionAsync(
            string adbSerial,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(adbSerial))
                throw new ArgumentException(nameof(adbSerial));

            var deviceLock = _deviceLocks.GetOrAdd(
                adbSerial.Trim(),
                _ => new SemaphoreSlim(1, 1));

            await deviceLock.WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            return new LockedDeviceSession(
                adbSerial.Trim(),
                _adbClient,
                deviceLock);
        }

        /// <summary>
        /// Wraps DeviceSession and guarantees lock release on dispose.
        /// Must implement IDeviceSession exactly (including timeout overloads).
        /// </summary>
        private sealed class LockedDeviceSession : IDeviceSession
        {
            private readonly IDeviceSession _inner;
            private readonly SemaphoreSlim _lock;
            private bool _disposed;

            public LockedDeviceSession(
                string adbSerial,
                IAdbClient adbClient,
                SemaphoreSlim deviceLock)
            {
                // inner session is the real implementation (DeviceSession)
                _inner = new DeviceSession(adbSerial, adbClient);
                _lock = deviceLock;
            }

            public string AdbSerial => _inner.AdbSerial;

            public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
                => _inner.WaitUntilReadyAsync(cancellationToken);

            public Task<string> ShellAsync(
                string command,
                int timeoutMs = 12_000,
                CancellationToken cancellationToken = default)
                => _inner.ShellAsync(command, timeoutMs, cancellationToken);

            public Task StartActivityAsync(
                string component,
                int timeoutMs = 20_000,
                CancellationToken cancellationToken = default)
                => _inner.StartActivityAsync(component, timeoutMs, cancellationToken);

            public Task<bool> IsPackageRunningAsync(
                string packageName,
                int timeoutMs = 8_000,
                CancellationToken cancellationToken = default)
                => _inner.IsPackageRunningAsync(packageName, timeoutMs, cancellationToken);

            public Task ForceStopAsync(
                string packageName,
                int timeoutMs = 8_000,
                CancellationToken cancellationToken = default)
                => _inner.ForceStopAsync(packageName, timeoutMs, cancellationToken);

            public async ValueTask DisposeAsync()
            {
                if (_disposed)
                    return;

                _disposed = true;

                try
                {
                    await _inner.DisposeAsync().ConfigureAwait(false);
                }
                finally
                {
                    _lock.Release();
                }
            }
        }
    }
}

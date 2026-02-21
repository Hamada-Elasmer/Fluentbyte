/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Services/AdbMapping.cs
 * Purpose: Library component: AdbMapping.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdbLib.Abstractions;
using AdbLib.Exceptions;
using AdbLib.Models;
using AdbLib.Options;

namespace AdbLib.Services;

public sealed class AdbMapping : IAdbMapping
{
    private readonly IAdbClient _adb;
    private readonly AdbOptions _opt;

    // runtime cache (you can also persist in your settings later)
    private readonly ConcurrentDictionary<int, string> _cache = new();

    public AdbMapping(IAdbClient adb, AdbOptions options)
    {
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
        _opt = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IReadOnlyDictionary<int, string>> BuildMappingAsync(
        IReadOnlyList<int> instanceIds,
        Func<int, Task> launchInstanceAsync,
        Func<int, Task> stopInstanceAsync,
        CancellationToken ct)
    {
        if (instanceIds is null || instanceIds.Count == 0)
            return new Dictionary<int, string>();

        if (launchInstanceAsync is null) throw new ArgumentNullException(nameof(launchInstanceAsync));
        if (stopInstanceAsync is null) throw new ArgumentNullException(nameof(stopInstanceAsync));

        // Start server to ensure consistent results
        TryRestartServer();

        // Baseline devices
        var baseline = _adb.Devices().Select(d => d.Serial).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<int, string>();

        foreach (var id in instanceIds)
        {

            ct.ThrowIfCancellationRequested();

            // Launch one instance at a time, and always attempt to stop it even if mapping fails.
            await launchInstanceAsync(id);

            try
            {
                // Wait for "new" serial to appear as device
                var serial = await WaitForNewDeviceSerialAsync(baseline, ct);

                // Cache + result
                _cache[id] = serial;
                result[id] = serial;

                // Update baseline: include the one we just found
                baseline.Add(serial);
            }
            finally
            {
                try
                {
                    // Stop instance to keep environment clean
                    await stopInstanceAsync(id);
                }
                catch
                {
                    // ignore stop failures; mapping should still return what we have
                }
            }

            // Let adb list stabilize
            await Task.Delay(800, ct);

        }

        return result;
    }

    public async Task<string> ResolveSerialForInstanceAsync(
        int instanceId,
        Func<int, Task> launchInstanceAsync,
        Func<int, Task> stopInstanceAsync,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(instanceId, out var serial) && !string.IsNullOrWhiteSpace(serial))
            return serial;

        // Build mapping for this one instance only
        var map = await BuildMappingAsync(
            new[] { instanceId },
            launchInstanceAsync,
            stopInstanceAsync,
            ct);

        if (map.TryGetValue(instanceId, out serial) && !string.IsNullOrWhiteSpace(serial))
            return serial;

        throw new AdbMappingException($"Failed to resolve ADB serial for instanceId={instanceId}");
    }

    private void TryRestartServer()
    {
        try
        {
            _adb.KillServer();
            _adb.StartServer();
        }
        catch
        {
            // ignore; mapping can still work if server already running
        }
    }
    private async Task<string> WaitForNewDeviceSerialAsync(
        HashSet<string> baselineSerials,
        CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(_opt.DeviceWaitTimeoutMs);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            IReadOnlyList<AdbDevice> devices;
            try
            {
                devices = _adb.Devices();
            }
            catch
            {
                // transient adb issues; wait and retry
                await Task.Delay(_opt.DevicePollIntervalMs, ct);
                continue;
            }

            // Pick first device not in baseline AND state==device
            var candidate = devices.FirstOrDefault(d =>
                d.State.Equals("device", StringComparison.OrdinalIgnoreCase) &&
                !baselineSerials.Contains(d.Serial));

            if (candidate is not null)
                return candidate.Serial;

            await Task.Delay(_opt.DevicePollIntervalMs, ct);
        }

        throw new AdbDeviceNotFoundException("No new adb device appeared within timeout (diff-based mapping).");
    }
}
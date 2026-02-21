/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Emulator/IEmulatorInstanceControlService.cs
 * Purpose: Abstraction for emulator instance lifecycle control.
 * Notes:
 *  - InstanceId is string (matches EmulatorLib).
 * ============================================================================ */


using EmulatorLib.Abstractions;
using EmulatorLib.Models;

namespace SparkFlow.Abstractions.Services.Emulator.Guards;

public interface IEmulatorInstanceControlService
{
    Task RefreshAsync(CancellationToken ct = default);

    IEmulatorInstance GetById(string instanceId);
    IEmulatorInstance? TryGetById(string instanceId);

    Task StartAsync(
        string instanceId,
        bool waitUntilRunning = true,
        int timeoutMs = 90_000,
        CancellationToken ct = default);

    Task StopAsync(
        string instanceId,
        bool waitUntilStopped = true,
        int timeoutMs = 60_000,
        CancellationToken ct = default);

    Task<IReadOnlyList<EmulatorInstanceInfo>> List2Async(CancellationToken ct = default);

    Task EmergencyStopAllAsync(CancellationToken ct = default);

    Task EmergencyStopAllAsync(
        int overallTimeoutMs,
        int perInstanceTimeoutMs = 7000,
        int maxParallelStops = 3,
        CancellationToken ct = default);
}

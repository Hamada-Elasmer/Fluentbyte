using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Runner;

public static class NextRunPolicy
{
    private static readonly Random _rng = new();

    public static DateTimeOffset ComputeNextRunAt(
        DateTimeOffset nowUtc,
        bool success,
        RunnerFailureType failureType,
        int consecutiveFailures)
    {
        // Base intervals (tune later per product)
        var baseSuccess = TimeSpan.FromMinutes(12);
        var baseFailure = TimeSpan.FromMinutes(2);

        // Success: stable schedule with light jitter
        if (success)
        {
            return nowUtc + baseSuccess + JitterSeconds(20);
        }

        // Missing serial: long disable pattern
        if (failureType == RunnerFailureType.Skipped_NoSerial)
        {
            return nowUtc + TimeSpan.FromMinutes(30) + JitterSeconds(60);
        }

        // Failure: exponential-ish backoff, capped
        // 1->2m, 2->3m, 3->5m, 4->10m, 5+->20m
        var backoff = consecutiveFailures switch
        {
            <= 1 => baseFailure,
            2 => TimeSpan.FromMinutes(3),
            3 => TimeSpan.FromMinutes(5),
            4 => TimeSpan.FromMinutes(10),
            _ => TimeSpan.FromMinutes(20)
        };

        // Heavier failures get slightly longer
        if (failureType is RunnerFailureType.AdbFailure or RunnerFailureType.DeviceUnreachable)
            backoff += TimeSpan.FromMinutes(3);

        if (failureType is RunnerFailureType.DeviceReadyTimeout)
            backoff += TimeSpan.FromMinutes(2);

        // Cap
        if (backoff > TimeSpan.FromMinutes(45))
            backoff = TimeSpan.FromMinutes(45);

        return nowUtc + backoff + JitterSeconds(45);
    }

    public static TimeSpan? ComputeDisableDuration(int consecutiveFailures, RunnerFailureType type)
    {
        // Disable when repeated failures (enterprise safety)
        if (consecutiveFailures < 3) return null;

        // Escalate disables
        return consecutiveFailures switch
        {
            3 => TimeSpan.FromMinutes(5),
            4 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(60)
        };
    }

    private static TimeSpan JitterSeconds(int maxSeconds)
    {
        lock (_rng)
        {
            return TimeSpan.FromSeconds(_rng.Next(0, Math.Max(1, maxSeconds + 1)));
        }
    }
}
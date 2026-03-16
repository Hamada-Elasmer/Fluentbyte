using SparkFlow.Domain.Models.Runner;

namespace SparkFlow.Engine.Runner;

public static class AutoDisablePolicy
{
    public static TimeSpan? GetDisableDuration(int consecutiveFailures, RunnerFailureType type)
    {
        // Enterprise-friendly: short cool downs then exponential
        if (type == RunnerFailureType.Skipped_NoSerial)
            return TimeSpan.FromMinutes(30);

        if (consecutiveFailures < 3)
            return null;

        // 3 -> 5 min, 4 -> 15 min, 5+ -> 60 min
        return consecutiveFailures switch
        {
            3 => TimeSpan.FromMinutes(5),
            4 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(60)
        };
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Engine/Engine/Scheduler/WaitLogic.cs
 * Purpose: Core component: WaitLogic.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Engine.Interfaces;

namespace SparkFlow.Engine.Engine.Scheduler;

/// <summary>
/// Professional, safe default: exponential backoff with caps.
/// attemptNumber starts at 1.
/// </summary>
public sealed class WaitLogic : IWaitLogic
{
    private readonly TimeSpan _minDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _multiplier;

    public WaitLogic()
        : this(
            minDelay: TimeSpan.FromMilliseconds(350),
            maxDelay: TimeSpan.FromSeconds(10),
            multiplier: 1.6)
    {
    }

    public WaitLogic(TimeSpan minDelay, TimeSpan maxDelay, double multiplier = 1.6)
    {
        if (minDelay < TimeSpan.Zero) minDelay = TimeSpan.Zero;
        if (maxDelay < minDelay) maxDelay = minDelay;
        if (multiplier < 1.0) multiplier = 1.0;

        _minDelay = minDelay;
        _maxDelay = maxDelay;
        _multiplier = multiplier;
    }

    public TimeSpan GetDelay(int attemptNumber)
    {
        if (attemptNumber <= 1) return _minDelay;

        // exponential: min * multiplier^(attempt-1)
        var pow = Math.Pow(_multiplier, attemptNumber - 1);
        var ms = _minDelay.TotalMilliseconds * pow;

        if (ms > _maxDelay.TotalMilliseconds)
            ms = _maxDelay.TotalMilliseconds;

        if (ms < 0) ms = 0;

        return TimeSpan.FromMilliseconds(ms);
    }
}
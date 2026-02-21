/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Engine/Engine/Scheduler/ResourceCheckScheduler.cs
 * Purpose: Core component: ResourceCheckScheduler.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Diagnostics;
using SparkFlow.Abstractions.Engine.Interfaces;

namespace SparkFlow.Engine.Engine.Scheduler;

public sealed class ResourceCheckScheduler : IResourceCheckScheduler
{
    private readonly IWaitLogic _wait;

    public event Action<int>? CheckAttemptStarted;
    public event Action<int>? CheckSucceeded;
    public event Action<int>? CheckFailed;
    public event Action<int, Exception>? CheckErrored;

    public ResourceCheckScheduler(IWaitLogic wait)
    {
        _wait = wait ?? throw new ArgumentNullException(nameof(wait));
    }

    public async Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> checkAsync,
        CancellationToken ct)
    {
        if (checkAsync is null) throw new ArgumentNullException(nameof(checkAsync));

        var attempt = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            attempt++;

            CheckAttemptStarted?.Invoke(attempt);

            bool ok;
            try
            {
                ok = await checkAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Exception counts as failed attempt (and we keep retrying)
                CheckErrored?.Invoke(attempt, ex);
                ok = false;
            }

            if (ok)
            {
                CheckSucceeded?.Invoke(attempt);
                return;
            }

            CheckFailed?.Invoke(attempt);

            var delay = _wait.GetDelay(attempt);
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> TryWaitUntilAsync(
        Func<CancellationToken, Task<bool>> checkAsync,
        TimeSpan timeout,
        CancellationToken ct)
    {
        if (checkAsync is null) throw new ArgumentNullException(nameof(checkAsync));
        if (timeout <= TimeSpan.Zero) timeout = TimeSpan.FromMilliseconds(1);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        var attempt = 0;
        var sw = Stopwatch.StartNew();

        while (true)
        {
            timeoutCts.Token.ThrowIfCancellationRequested();
            attempt++;

            CheckAttemptStarted?.Invoke(attempt);

            bool ok;
            try
            {
                ok = await checkAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // If original ct canceled -> throw
                ct.ThrowIfCancellationRequested();
                // Otherwise timeout hit
                return false;
            }
            catch (Exception ex)
            {
                CheckErrored?.Invoke(attempt, ex);
                ok = false;
            }

            if (ok)
            {
                CheckSucceeded?.Invoke(attempt);
                return true;
            }

            CheckFailed?.Invoke(attempt);

            var remaining = timeout - sw.Elapsed;
            if (remaining <= TimeSpan.Zero)
                return false;

            var delay = _wait.GetDelay(attempt);
            if (delay <= TimeSpan.Zero)
                continue;

            if (delay > remaining)
                delay = remaining;

            try
            {
                await Task.Delay(delay, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                ct.ThrowIfCancellationRequested();
                return false; // timeout
            }
        }
    }
}
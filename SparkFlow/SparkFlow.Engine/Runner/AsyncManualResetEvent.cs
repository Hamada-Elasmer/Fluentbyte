/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Services/Runner/AsyncManualResetEvent.cs
 * Purpose: Core component: AsyncManualResetEvent.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Engine.Runner;

internal sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> _tcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public AsyncManualResetEvent(bool initialState)
    {
        if (initialState)
            _tcs.TrySetResult(true);
    }

    public Task WaitAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled(ct);

        var task = _tcs.Task;
        if (task.IsCompleted)
            return task;

        // Attach cancellation
        return WaitWithCancellation(task, ct);

        static async Task WaitWithCancellation(Task task, CancellationToken ct2)
        {
            var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = ct2.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), cancelTcs);

            if (task == await Task.WhenAny(task, cancelTcs.Task).ConfigureAwait(false))
            {
                await task.ConfigureAwait(false);
                return;
            }

            throw new OperationCanceledException(ct2);
        }
    }

    public void Set() => _tcs.TrySetResult(true);

    public void Reset()
    {
        while (true)
        {
            var tcs = _tcs;
            if (!tcs.Task.IsCompleted)
                return;

            var newTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (Interlocked.CompareExchange(ref _tcs, newTcs, tcs) == tcs)
                return;
        }
    }

    public bool IsSet => _tcs.Task.IsCompleted;
}
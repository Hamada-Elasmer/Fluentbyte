/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Engine/Engine/MultiAccount/RotationManager.cs
 * Purpose: Core component: RotationManager.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Abstractions.State.Interfaces;
using SparkFlow.Engine.Runner;

namespace SparkFlow.Engine.Engine.MultiAccount;

public sealed class RotationManager : IRotationManager
{
    private readonly IAccountQueue _queue;

    private readonly object _gate = new();

    private readonly AsyncManualResetEvent _pauseEvent = new(initialState: true);

    public IAccountState? Current { get; private set; }

    public bool IsPaused { get; private set; }

    public event Action<IAccountState>? AccountStarted;
    public event Action<IAccountState>? AccountFinished;
    public event Action<IAccountState, Exception>? AccountFailed;
    public event Action? RotationCompleted;

    public RotationManager(IAccountQueue queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    public void Pause()
    {
        lock (_gate)
        {
            if (IsPaused) return;
            IsPaused = true;
            _pauseEvent.Reset();
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            if (!IsPaused) return;
            IsPaused = false;
            _pauseEvent.Set();
        }
    }

    public async Task PausePointAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _pauseEvent.WaitAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
    }

    public async Task RunOnceAsync(
        Func<IAccountState, CancellationToken, Task> runAccountAsync,
        CancellationToken ct)
    {
        if (runAccountAsync is null) throw new ArgumentNullException(nameof(runAccountAsync));

        // NOTE:
        // Caller is responsible for preparing queue (Rebuild/Reset) before calling RunOnceAsync.
        // We will iterate until Next() returns null.
        while (true)
        {
            await PausePointAsync(ct).ConfigureAwait(false);

            IAccountState? next;
            try
            {
                next = _queue.Next();
            }
            catch (Exception ex)
            {
                // Queue failure: treat as fatal for this rotation pass.
                Current = null;
                RotationCompleted?.Invoke();
                throw new InvalidOperationException("Account queue failed while retrieving Next().", ex);
            }

            if (next is null)
            {
                Current = null;
                RotationCompleted?.Invoke();
                return;
            }

            // ✅ Respect ON/OFF toggle (Enabled)
            if (!next.Enabled)
                continue;

            // ✅ Treat invalid instance ids as "skip" (prevents instance 0 bug)
            var instanceId = string.IsNullOrWhiteSpace(next.InstanceId) ? "" : next.InstanceId.Trim();
            if (instanceId.Length == 0 || instanceId == "-1" || instanceId == "0")
                continue;

            Current = next;
            AccountStarted?.Invoke(next);

            try
            {
                await PausePointAsync(ct).ConfigureAwait(false);
                await runAccountAsync(next, ct).ConfigureAwait(false);
                await PausePointAsync(ct).ConfigureAwait(false);

                AccountFinished?.Invoke(next);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is "hard stop"
                throw;
            }
            catch (Exception ex)
            {
                AccountFailed?.Invoke(next, ex);
                // Skip-on-failure policy: continue to next account
            }
        }
    }
}

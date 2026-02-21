/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.Core/Services/Game/GameRunner.cs
 * Purpose: Core component: Executes a game module against a platform run context.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using GameContracts.Abstractions;
using GameContracts.Common;
using GameContracts.Tasks;
using SparkFlow.Abstractions.Abstractions;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Game;

public sealed class GameRunner
{
    private readonly MLogger _log;

    public GameRunner(MLogger logger)
    {
        _log = logger ?? MLogger.Instance;
    }

    public async Task RunAsync(
        IGameModule module,
        IRunContext run,
        int sessionId,
        string runId)
    {
        if (module is null) throw new ArgumentNullException(nameof(module));
        if (run is null) throw new ArgumentNullException(nameof(run));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException(nameof(runId));

        var ct = run.CancellationToken;

        // ✅ Ensure session is ready before invoking the module
        await run.Device.WaitUntilReadyAsync(ct).ConfigureAwait(false);

        var gameCtx = GameContextAdapter.FromRunContext(run);

        _log.Log(
            LogComponent.Game,
            LogChannel.GAME,
            LogLevel.INFO,
            $"[GameRunner] Start | Game={module.GameId} | Serial={run.Device.AdbSerial}",
            sessionId,
            runId,
            run.Profile.Id);

        try
        {
            // 1) Detect installed (optional)
            var isInstalled = true; // default true so we don't block if detector can't decide
            try
            {
                isInstalled = await module.Detector.IsInstalledAsync(gameCtx, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Detector failed -> continue (NO INSTALL MODE)
                isInstalled = true;

                _log.Log(
                    LogComponent.Game,
                    LogChannel.GAME,
                    LogLevel.WARNING,
                    $"[GameRunner] Detector.IsInstalled failed: {ex.Message}",
                    sessionId,
                    runId,
                    run.Profile.Id);
            }

            // ✅ NO INSTALL MODE: if not installed -> skip
            if (!isInstalled)
            {
                _log.Log(
                    LogComponent.Game,
                    LogChannel.GAME,
                    LogLevel.WARNING,
                    $"[GameRunner] Game not installed -> skipping (NO INSTALL MODE) | Game={module.GameId}",
                    sessionId,
                    runId,
                    run.Profile.Id);

                return;
            }

            // 2) Launch
            await module.Lifecycle.LaunchAsync(gameCtx, ct).ConfigureAwait(false);

            // 3) Wait ready
            await module.Lifecycle.WaitUntilReadyAsync(gameCtx, ct).ConfigureAwait(false);

            // 4) Tasks (sequential)
            foreach (var task in module.Tasks ?? Enumerable.Empty<IGameTask>())
            {
                ct.ThrowIfCancellationRequested();
                await ExecuteTaskAsync(task, gameCtx, ct, sessionId, runId, run.Profile.Id)
                    .ConfigureAwait(false);
            }

            _log.Log(
                LogComponent.Game,
                LogChannel.GAME,
                LogLevel.INFO,
                $"[GameRunner] Done | Game={module.GameId}",
                sessionId,
                runId,
                run.Profile.Id);
        }
        finally
        {
            // 5) Module shutdown (optional)
            try
            {
                await module.Lifecycle.ShutdownAsync(gameCtx, ct)
                    .ConfigureAwait(false);
            }
            catch (NotImplementedException)
            {
                // optional
            }
            catch (Exception ex)
            {
                _log.Log(
                    LogComponent.Game,
                    LogChannel.GAME,
                    LogLevel.WARNING,
                    $"[GameRunner] Shutdown failed: {ex.Message}",
                    sessionId,
                    runId,
                    run.Profile.Id);
            }

            // ❌ IMPORTANT: Do NOT dispose the device session here.
            // Session lifecycle (and instance closing) is owned by the Runner.
        }
    }

    private async Task ExecuteTaskAsync(
        IGameTask task,
        GameContext gameCtx,
        CancellationToken ct,
        int sessionId,
        string runId,
        string profileId)
    {
        _log.Log(
            LogComponent.Game,
            LogChannel.GAME,
            LogLevel.INFO,
            $"[Task] Start | {task.TaskId}",
            sessionId,
            runId,
            profileId);

        GameTaskResult? result = null;

        try
        {
            result = await task.ExecuteAsync(gameCtx, ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log.Exception(
                LogComponent.Game,
                LogChannel.GAME,
                ex,
                $"[Task] Exception | {task.TaskId}",
                sessionId,
                runId,
                profileId);
            return;
        }

        var msg = result?.Message ?? string.Empty;

        if (result is not null && result.Success)
        {
            _log.Log(
                LogComponent.Game,
                LogChannel.GAME,
                LogLevel.INFO,
                $"[Task] Success | {task.TaskId} | {msg}",
                sessionId,
                runId,
                profileId);
        }
        else
        {
            _log.Log(
                LogComponent.Game,
                LogChannel.GAME,
                LogLevel.WARNING,
                $"[Task] Failed | {task.TaskId} | {msg}",
                sessionId,
                runId,
                profileId);
        }
    }
}

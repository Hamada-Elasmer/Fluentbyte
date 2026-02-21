/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Internal/ProcessExec.cs
 * Purpose: Library component: ProcessExec.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdbLib.Internal;

/// <summary>
/// Small, dependency-free process runner that captures stdout/stderr asynchronously
/// to avoid deadlocks caused by full output buffers.
/// </summary>
internal static class ProcessExec
{
    internal sealed record Result(int ExitCode, string StdOut, string StdErr);

    public static Result Run(string fileName, string arguments, string? workingDir, int timeoutMs)
        => RunAsync(fileName, arguments, workingDir, timeoutMs, CancellationToken.None).GetAwaiter().GetResult();

    public static Result Run(string fileName, IEnumerable<string> args, string? workingDir, int timeoutMs)
        => RunAsync(fileName, args, workingDir, timeoutMs, CancellationToken.None).GetAwaiter().GetResult();

    public static async Task<Result> RunAsync(string fileName, string arguments, string? workingDir, int timeoutMs, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDir ?? string.Empty,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        return await RunInternalAsync(psi, timeoutMs, ct).ConfigureAwait(false);
    }

    public static async Task<Result> RunAsync(string fileName, IEnumerable<string> args, string? workingDir, int timeoutMs, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDir ?? string.Empty,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (args != null)
        {
            foreach (var a in args)
                psi.ArgumentList.Add(a);
        }

        return await RunInternalAsync(psi, timeoutMs, ct).ConfigureAwait(false);
    }

    private static async Task<Result> RunInternalAsync(ProcessStartInfo psi, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0) timeoutMs = 30_000;

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var stdoutTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) stdoutTcs.TrySetResult(true);
            else stdout.AppendLine(e.Data);
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) stderrTcs.TrySetResult(true);
            else stderr.AppendLine(e.Data);
        };

        try
        {
            if (!p.Start())
                throw new InvalidOperationException($"Failed to start process: {psi.FileName}");

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                await p.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // Timeout path: kill and throw TimeoutException (existing behavior)
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                throw new TimeoutException($"Process timed out after {timeoutMs} ms: {psi.FileName} {psi.Arguments}");
            }
            catch (OperationCanceledException)
            {
                // ✅ NEW: cancellation path (Stop) - kill process to avoid orphan adb.exe
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                throw;
            }

            // Ensure async readers completed
            await Task.WhenAll(stdoutTcs.Task, stderrTcs.Task).ConfigureAwait(false);

            return new Result(p.ExitCode, stdout.ToString().Trim(), stderr.ToString().Trim());
        }
        finally
        {
            try { p.CancelOutputRead(); } catch { }
            try { p.CancelErrorRead(); } catch { }
        }
    }
}
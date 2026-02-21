/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Impl/DefaultProcessRunner.cs
 * Purpose: Library component: DefaultProcessRunner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdbLib.Abstractions;

namespace AdbLib.Impl;

public sealed class DefaultProcessRunner : IProcessRunner
{
    public async Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string fileName,
        string arguments,
        int timeoutMs = 30_000,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        p.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        p.Exited += (_, __) => tcs.TrySetResult(p.ExitCode);

        if (!p.Start())
            return (-1, "", "Failed to start process.");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            var exitCode = await tcs.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            return (exitCode, stdout.ToString(), stderr.ToString());
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            throw;
        }
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/ProcessRunner.cs
 * Purpose: Library component: ProcessRunner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Diagnostics;
using System.Text;

namespace UtiliLib;

public sealed class ProcessRunner
{
    public (int exitCode, string stdout, string stderr) Run(
        string fileName,
        string arguments,
        string workingDir,
        int timeoutMs)
    {
        return RunAsync(fileName, arguments, workingDir, timeoutMs).GetAwaiter().GetResult();
    }

    public async Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string fileName,
        string arguments,
        string workingDir,
        int timeoutMs,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        p.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        p.Exited += (_, __) => tcs.TrySetResult(p.ExitCode);

        if (!p.Start())
            throw new InvalidOperationException($"Failed to start process: {fileName}");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        using var timeoutCts = timeoutMs > 0 ? new CancellationTokenSource(timeoutMs) : new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            var exitCode = await tcs.Task.WaitAsync(linked.Token).ConfigureAwait(false);
            return (exitCode, stdout.ToString(), stderr.ToString());
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            if (ct.IsCancellationRequested)
                throw;

            throw new TimeoutException($"Process timeout: {fileName} {arguments}");
        }
    }
}
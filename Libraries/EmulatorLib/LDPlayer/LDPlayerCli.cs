/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/EmulatorLib/LDPlayer/LDPlayerCli.cs
 * Purpose: LDPlayer CLI wrapper (dnconsole / ldconsole).
 * Notes:
 *  - Runs dnconsole commands with stdout/stderr capture.
 *  - dnconsole may return non-zero exit code with valid stdout (e.g., list2).
 * ============================================================================ */

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

using UtiliLib;
using UtiliLib.Types;

namespace EmulatorLib.LDPlayer;

[SupportedOSPlatform("windows")]
public sealed class LDPlayerCli
{
    private readonly string _exe;

    public LDPlayerCli(string dnConsolePath)
    {
        _exe = dnConsolePath;
    }

    public string Run(string args, int timeoutMs = 30_000)
    {
        var wd = Path.GetDirectoryName(_exe);

        var psi = new ProcessStartInfo
        {
            FileName = _exe,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(wd) ? Environment.CurrentDirectory : wd
        };

        try
        {
            using var p = new Process { StartInfo = psi };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            p.Start();

            stdout.Append(p.StandardOutput.ReadToEnd());
            stderr.Append(p.StandardError.ReadToEnd());

            if (!p.WaitForExit(timeoutMs))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignored */ }
                throw new TimeoutException("dnconsole timeout");
            }

            var outText = stdout.ToString();
            var errText = stderr.ToString();

            // Diagnostics (important for your case)
            MLogger.Instance.Debug(
                LogChannel.SYSTEM,
                $"[LDPlayerCli] {Path.GetFileName(_exe)} {args} | ExitCode={p.ExitCode} | stdout_len={outText.Length} | stderr_len={errText.Length}");

            // dnconsole may return non-zero exit code while still producing valid stdout (e.g., list2).
            if (p.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(outText))
                    return outText;

                throw new InvalidOperationException(
                    $"dnconsole failed (ExitCode={p.ExitCode}). stderr: {errText}");
            }

            return outText;
        }
        catch (Exception ex)
        {
            MLogger.Instance.Error(LogChannel.SYSTEM, $"[LDPlayerCli] Run FAILED | Exe='{_exe}' | Args='{args}' | Error={ex}");
            throw;
        }
    }

    public string List2() => Run("list2");
    public string RunningList() => Run("runninglist");

    public void Launch(string id) => Run($"launch --index {id}");
    public void Quit(string id) => Run($"quit --index {id}");

    // Optional convenience: reboot instance
    public void Reboot(string id) => Run($"reboot --index {id}");

    // ✅ Key addition: LDPlayer internal Android resolution (width,height,dpi)
    public void SetResolution(string id, int width, int height, int dpi = 320)
        => Run($"modify --index {id} --resolution {width},{height},{dpi}");

    // ✅ NEW: Enable ADB (persisted by LDPlayer config)
    public void EnableAdb(string id) => Run($"modify --index {id} --adb on");

    public void Copy(string templateId, string name)
        => Run($"copy --index {templateId} --name \"{name}\"");
}
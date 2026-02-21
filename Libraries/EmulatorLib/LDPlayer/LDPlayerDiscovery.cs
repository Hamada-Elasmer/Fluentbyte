using System.Runtime.Versioning;

namespace EmulatorLib.LDPlayer;

[SupportedOSPlatform("windows")]
public static class LDPlayerDiscovery
{
    /// <summary>
    /// Detects LDPlayer console executable.
    /// Supports both dnconsole.exe and ldconsole.exe (different LDPlayer builds).
    /// </summary>
    public static string? DetectConsoleExe()
    {
        var env = Environment.GetEnvironmentVariable("LDPLAYER_HOME");
        if (!string.IsNullOrWhiteSpace(env))
        {
            foreach (var exe in LDPlayerPaths.CandidateConsoleExes)
            {
                var p = Path.Combine(env, exe);
                if (File.Exists(p)) return p;
            }
        }

        foreach (var root in LDPlayerPaths.CommonRoots)
        {
            foreach (var exe in LDPlayerPaths.CandidateConsoleExes)
            {
                var p = Path.Combine(root, exe);
                if (File.Exists(p)) return p;
            }
        }

        return null;
    }
}

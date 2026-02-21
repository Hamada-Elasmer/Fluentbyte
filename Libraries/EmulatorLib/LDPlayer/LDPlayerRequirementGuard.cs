using System.Runtime.Versioning;
using EmulatorLib.Abstractions;

namespace EmulatorLib.LDPlayer;

[SupportedOSPlatform("windows")]
public sealed class LDPlayerRequirementGuard : IEmulatorRequirementGuard
{
    public EmulatorRequirementResult Validate()
    {
        var path = LDPlayerDiscovery.DetectConsoleExe();
        if (path is null)
        {
            return EmulatorRequirementResult.Blocked(
                "LDPlayer Required",
                "LDPlayer is required to run SparkFlow.",
                "Install LDPlayer 9",
                "Ensure dnconsole.exe exists",
                "Restart SparkFlow");
        }

        return EmulatorRequirementResult.Ok();
    }
}

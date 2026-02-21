using GameContracts.Common;
using SparkFlow.Abstractions.Abstractions;

namespace SparkFlow.Infrastructure.Services.Game;

internal static class GameContextAdapter
{
    public static GameContext FromRunContext(IRunContext run)
    {
        if (run is null)
            throw new ArgumentNullException(nameof(run));

        return new GameContext
        {
            ProfileId = run.Profile.Id,
            GameId = run.GameId,

            // ✅ ADB serial is the device identifier for game modules
            DeviceId = run.Device?.AdbSerial
        };
    }
}

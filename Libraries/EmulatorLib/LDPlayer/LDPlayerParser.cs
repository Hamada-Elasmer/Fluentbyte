using EmulatorLib.Abstractions;
using EmulatorLib.Models;

namespace EmulatorLib.LDPlayer;

public static class LDPlayerParser
{
    public static IReadOnlyList<EmulatorInstanceInfo> Parse(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout))
            return Array.Empty<EmulatorInstanceInfo>();

        var lines = stdout
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var list = new List<EmulatorInstanceInfo>(capacity: lines.Length);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            // LDPlayer list2 is CSV-like
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;

            var id = parts[0];
            var name = parts[1];

            // Best-effort ADB port detection:
            // Find any integer >= 5555 in remaining fields.
            int? port = null;
            for (var i = 2; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out var n) && n >= 5555)
                {
                    port = n;
                    break;
                }
            }

            list.Add(new EmulatorInstanceInfo(
                id,
                name,
                port,
                EmulatorState.Unknown,
                line));
        }

        return list;
    }
}

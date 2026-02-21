/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/EmulatorLib/LDPlayer/LDPlayerInstance.cs
 * Purpose: Emulator instance: LDPlayerInstance.
 * Notes:
 *  - Implements IEmulatorInstance : IAdbPortProvider exactly as defined in Fluentbyte.
 * ============================================================================ */

using System.Runtime.Versioning;
using EmulatorLib.Abstractions;
using EmulatorLib.Models;

namespace EmulatorLib.LDPlayer;

[SupportedOSPlatform("windows")]
public sealed class LDPlayerInstance : IEmulatorInstance, IAdbPortProvider
{
    private readonly LDPlayerCli _cli;
    private readonly EmulatorInstanceInfo _info;

    public LDPlayerInstance(LDPlayerCli cli, EmulatorInstanceInfo info)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
        _info = info ?? throw new ArgumentNullException(nameof(info));
    }

    public string InstanceId => _info.InstanceId;
    public string Name => _info.Name;

    public EmulatorState State => _info.State;

    /// <summary>
    /// Best-effort ADB port.
    /// - Prefer parser-provided port if valid.
    /// - Fallback for LDPlayer9 TCP: index 0->5555, 1->5557, 2->5559 ...
    /// </summary>
    public int? AdbPort
    {
        get
        {
            // If parser already extracted a port, accept it if it looks like an ADB TCP port.
            // (LDPlayer usually uses >= 5555 for TCP)
            if (_info.AdbPort is int p && p >= 5555)
                return p;

            var idx = TryParseInstanceIndex(_info.InstanceId);
            if (idx >= 0)
                return 5555 + (idx * 2); // ✅ LDPlayer9 TCP pattern

            return null;
        }
    }

    /// <summary>Starts the instance (launch).</summary>
    public void Start() => _cli.Launch(InstanceId);

    /// <summary>Stops the instance (quit).</summary>
    public void Stop() => _cli.Quit(InstanceId);

    /// <summary>
    /// Applies LDPlayer internal Android resolution (width/height/dpi).
    /// Best applied while instance is stopped (pre-launch).
    /// </summary>
    public void SetAndroidResolution(int width, int height, int dpi = 320)
        => _cli.SetResolution(InstanceId, width, height, dpi);

    /// <summary>Enable ADB via LDPlayer modify command.</summary>
    public void EnableAdb() => _cli.EnableAdb(InstanceId);

    public async Task WaitUntilOnlineAsync(CancellationToken ct)
    {
        // Minimal, deterministic wait loop (LDPlayer list2 port is best-effort).
        if (AdbPort is null)
            throw new InvalidOperationException(
                "ADB port missing for this instance (list2 did not provide a valid port and fallback failed).");

        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(500, ct).ConfigureAwait(false);
        }
    }

    private static int TryParseInstanceIndex(string? instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return -1;

        instanceId = instanceId.Trim();

        // ✅ Allow 0-based indexes ("0", "1", "2", ...)
        if (int.TryParse(instanceId, out var n) && n >= 0)
            return n;

        // Best-effort: extract trailing digits (e.g., "leidian3" -> 3)
        var i = instanceId.Length - 1;
        while (i >= 0 && char.IsDigit(instanceId[i])) i--;

        var digits = instanceId[(i + 1)..];
        return (int.TryParse(digits, out var t) && t >= 0) ? t : -1;
    }
}
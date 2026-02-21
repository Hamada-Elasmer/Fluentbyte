/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/EmulatorLib/LDPlayer/LDPlayerEmulator.cs
 * Purpose: Emulator implementation: LDPlayerEmulator.
 * Notes:
 *  - Implements EmulatorLib.Abstractions.IEmulator (LDPlayer-only).
 *  - Best-effort scan: never throws to UI callers.
 * ============================================================================ */

using System.Runtime.Versioning;

using EmulatorLib.Abstractions;

using UtiliLib;
using UtiliLib.Types;

namespace EmulatorLib.LDPlayer;

[SupportedOSPlatform("windows")]
public sealed class LDPlayerEmulator : IEmulator
{
    private readonly LDPlayerCli _cli;
    private readonly LDPlayerTemplateService _template;

    private List<IEmulatorInstance> _cache = new();

    public LDPlayerEmulator(
        LDPlayerCli cli,
        LDPlayerTemplateService template)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
        _template = template ?? throw new ArgumentNullException(nameof(template));
    }

    public bool IsInstalled
    {
        get
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                _ = _cli.List2();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public IReadOnlyList<IEmulatorInstance> ScanInstances()
    {
        try
        {
            var raw = _cli.List2();

            if (string.IsNullOrWhiteSpace(raw))
            {
                MLogger.Instance.Warn(LogChannel.SYSTEM,
                    "[Emu][LDPlayer] list2 returned EMPTY output.");
                return _cache;
            }

            var infos = LDPlayerParser.Parse(raw);

            _cache = infos
                .Select(i => (IEmulatorInstance)new LDPlayerInstance(_cli, i))
                .ToList();

            MLogger.Instance.Debug(LogChannel.SYSTEM,
                $"[Emu][LDPlayer] ScanInstances OK. Count={_cache.Count}");

            return _cache;
        }
        catch (Exception ex)
        {
            MLogger.Instance.Warn(LogChannel.SYSTEM,
                $"[Emu][LDPlayer] ScanInstances failed: {ex.Message}");

            return _cache;
        }
    }

    public IEmulatorInstance CreateInstanceFromTemplate(
        string templateInstanceId,
        string newName)
    {
        if (string.IsNullOrWhiteSpace(templateInstanceId))
            throw new ArgumentException("Template instance id is required.", nameof(templateInstanceId));

        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New instance name is required.", nameof(newName));

        _template.CloneFromTemplate(templateInstanceId, newName);

        var instances = ScanInstances();

        return instances.First(x =>
            string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase));
    }

    public IEmulatorInstance? TryGetInstance(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        if (_cache.Count == 0)
            _ = ScanInstances();

        return _cache.FirstOrDefault(x =>
            string.Equals(x.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
    }
}

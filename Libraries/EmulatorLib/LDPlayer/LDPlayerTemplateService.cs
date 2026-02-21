/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/EmulatorLib/LDPlayer/LDPlayerTemplateService.cs
 * Purpose: LDPlayer template clone helper (LDPlayer-only).
 * Notes:
 *  - Ensures template is stopped before cloning.
 * ============================================================================ */

using System.Runtime.Versioning;

namespace EmulatorLib.LDPlayer;

[SupportedOSPlatform("windows")]
public sealed class LDPlayerTemplateService
{
    private readonly LDPlayerCli _cli;

    public LDPlayerTemplateService(LDPlayerCli cli)
    {
        _cli = cli ?? throw new ArgumentNullException(nameof(cli));
    }

    public void EnsureTemplateStopped(string id)
    {
        var running = _cli.RunningList();
        if (!string.IsNullOrWhiteSpace(running) && running.Contains(id, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Template must be stopped before cloning.");
    }

    public void CloneFromTemplate(string templateId, string newName)
    {
        EnsureTemplateStopped(templateId);
        _cli.Copy(templateId, newName);
    }
}

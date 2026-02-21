/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Models/LogEntry.cs
 * Purpose: Library component: LogEntry.
 * Notes:
 *  - Component-based logging enabled via LogComponent.
 *  - Adds Source field for human-readable origin (Main/Api/Runner/...).
 *  - Backward-compatible with existing JSON and UI.
 * ============================================================================ */

using UtiliLib.Types;

namespace UtiliLib.Models;

public class LogEntry
{
    /// <summary>
    /// High-level component (Runner/Adb/Emulator/Api/Health/Ui/...)
    /// Used for routing (per-component files) and filtering.
    /// </summary>
    public LogComponent Component { get; set; }

    /// <summary>
    /// Source / category of the log (SYSTEM, UI, NETWORK, ...)
    /// </summary>
    public LogChannel Channel { get; set; }

    /// <summary>
    /// Human-readable origin label (Main/Api/Runner/GameRunner/...)
    /// Used for line formatting: CHANNEL|Source|Message
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Severity level (INFO, WARNING, ERROR, ...)
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Log message
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Unix timestamp in milliseconds (UTC)
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Session / numeric identifier (legacy / optional)
    /// </summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Run identifier for grouping all logs in one run (Guid string)
    /// </summary>
    public string RunId { get; set; }

    /// <summary>
    /// Profile identifier (Account/Profile Id) when applicable
    /// </summary>
    public string ProfileId { get; set; }

    public LogEntry()
    {
        Component = LogComponent.System;
        Channel = LogChannel.SYSTEM;

        // Default is safe and stable (matches your desired "Main" style)
        Source = "Main";

        Level = LogLevel.INFO;
        Text = string.Empty;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        SessionId = 0;

        RunId = string.Empty;
        ProfileId = string.Empty;
    }

    private static string ShortId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return string.Empty;

        id = id.Trim();
        return id.Length <= 8 ? id : id.Substring(0, 8);
    }

    public override string ToString()
    {
        // Note: ToString is mainly for UI/debug. File formatting is handled by Serilog template.
        var time = DateTimeOffset
            .FromUnixTimeMilliseconds(Timestamp)
            .ToLocalTime()
            .ToString("MM/dd/yyyy h:mm:ss tt");

        var parts = new List<string>(3);

        if (!string.IsNullOrWhiteSpace(RunId))
            parts.Add($"Run:{ShortId(RunId)}");

        if (SessionId != 0)
            parts.Add($"Session:{SessionId}");

        if (!string.IsNullOrWhiteSpace(ProfileId))
            parts.Add($"Profile:{ProfileId.Trim()}");

        var ctx = parts.Count == 0 ? "" : " " + string.Join(" ", parts);

        return $"[{Component}/{Channel}] ({time}) [{Level}] {Channel}|{Source}|{Text}{ctx}";
    }
}

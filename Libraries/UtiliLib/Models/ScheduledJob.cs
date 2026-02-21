/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Models/ScheduledJob.cs
 * Purpose: Library component: ScheduledJob.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace UtiliLib.Models;

/// <summary>
/// Represents a scheduled job with an action to run at specific intervals.
/// </summary>
public class ScheduledJob
{
    public string Name { get; set; }
    public Action Action { get; set; }
    public TimeSpan Interval { get; set; }
    public bool RunOnStart { get; set; }
    public bool PrintToLog { get; set; }

    public override string ToString() => Name;

    public ScheduledJob()
    {
        Name = string.Empty;
        Action = () => { };
        Interval = TimeSpan.Zero;
        RunOnStart = false;
        PrintToLog = false;
    }
}
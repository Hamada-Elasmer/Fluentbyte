/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Events/LogEventArgs.cs
 * Purpose: Library component: LogEventArgs.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using UtiliLib.Models;

namespace UtiliLib.Events;

public sealed class LogEventArgs : EventArgs
{
    public LogEntry LogEntry { get; }

    public LogEventArgs(LogEntry logEntry)
    {
        LogEntry = logEntry ?? throw new ArgumentNullException(nameof(logEntry));
    }
}
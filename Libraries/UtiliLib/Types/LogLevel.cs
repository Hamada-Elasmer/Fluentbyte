/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Types/LogLevel.cs
 * Purpose: Library component: LogLevel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace UtiliLib.Types;

/// <summary>
/// Represents the severity level of a log entry.
/// </summary>
public enum LogLevel
{
    INFO,
    WARNING,
    ERROR,
    DEBUG,
    EXCEPTION
}
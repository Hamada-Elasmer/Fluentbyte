/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Exceptions/AdbCommandException.cs
 * Purpose: Library component: AdbCommandException.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;

namespace AdbLib.Exceptions;

public sealed class AdbCommandException : Exception
{
    public int ExitCode { get; }
    public string StdOut { get; }
    public string StdErr { get; }

    public AdbCommandException(
        string message,
        int exitCode,
        string stdout,
        string stderr)
        : base(message)
    {
        ExitCode = exitCode;
        StdOut = stdout;
        StdErr = stderr;
    }
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Logging/LogContext.cs
 * Purpose: Library component: LogContextData.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Threading;

namespace UtiliLib.Logging;

public sealed class LogContextData
{
    public string? RunId { get; init; }
    public string? ProfileId { get; init; }
}

public static class LogContext
{
    private static readonly AsyncLocal<LogContextData?> _current = new();

    public static LogContextData? Current => _current.Value;

    public static IDisposable Push(LogContextData data)
    {
        var prev = _current.Value;
        _current.Value = data;
        return new PopDisposable(prev);
    }

    private sealed class PopDisposable : IDisposable
    {
        private readonly LogContextData? _prev;
        public PopDisposable(LogContextData? prev) => _prev = prev;
        public void Dispose() => _current.Value = _prev;
    }
}
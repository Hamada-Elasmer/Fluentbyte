/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Logging/LogSessionArchive.cs
 * Purpose: Core component: LogSessionArchive.
 * Notes:
 *  - Captures LogEntry events during a session and flushes them to JSON for analysis.
 *  - JSON archive is considered the "rich" storage (debugging/maintenance).
 *  - Subscription happens on service creation and is removed on Dispose().
 * ============================================================================ */

using System.Text.Json;
using UtiliLib;
using UtiliLib.Events;
using UtiliLib.Models;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Logging;

public sealed class LogSessionArchive : IDisposable
{
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = new();
    private readonly string _dir;

    private bool _disposed;

    public string SessionId { get; }
    public DateTimeOffset StartedUtc { get; }
    public DateTimeOffset? EndedUtc { get; private set; }

    public LogSessionArchive(string? logsDir = null)
    {
        StartedUtc = DateTimeOffset.UtcNow;
        SessionId = Guid.NewGuid().ToString("N")[..10];

        var baseDir = AppContext.BaseDirectory;
        _dir = logsDir ?? Path.Combine(baseDir, "runtime", "logs", "json");
        Directory.CreateDirectory(_dir);

        // Subscribe from app start (as soon as service is created)
        MLogger.Instance.LogEvent += OnLogEvent;

        MLogger.Instance.Info(LogChannel.SYSTEM,
            $"Log archive started. SessionId={SessionId}, StartedUtc={StartedUtc:O}");
    }

    private void OnLogEvent(object? sender, LogEventArgs e)
    {
        if (_disposed) return;

        lock (_lock)
        {
            _entries.Add(e.LogEntry);
        }
    }

    /// <summary>
    /// Writes the archived session logs to disk and returns the file path.
    /// </summary>
    public string FlushToFile()
    {
        if (_disposed)
            return string.Empty;

        EndedUtc = DateTimeOffset.UtcNow;

        List<LogEntry> snapshot;
        lock (_lock)
        {
            snapshot = new List<LogEntry>(_entries);
        }

        var fileName = $"sparkflow_session_{StartedUtc:yyyyMMdd_HHmmss}_utc_{SessionId}.json";
        var path = Path.Combine(_dir, fileName);

        var payload = new LogSessionFile
        {
            SessionId = SessionId,
            StartedUtc = StartedUtc,
            EndedUtc = EndedUtc.Value,
            Count = snapshot.Count,
            Entries = snapshot
        };

        var opt = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(path, JsonSerializer.Serialize(payload, opt));

        MLogger.Instance.Info(LogChannel.SYSTEM,
            $"Log archive flushed. SessionId={SessionId}, Count={snapshot.Count}, Path='{path}'");

        return path;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { MLogger.Instance.LogEvent -= OnLogEvent; }
        catch { /* ignored */ }
    }

    private sealed class LogSessionFile
    {
        public string SessionId { get; init; } = "";
        public DateTimeOffset StartedUtc { get; init; }
        public DateTimeOffset EndedUtc { get; init; }
        public int Count { get; init; }
        public List<LogEntry> Entries { get; init; } = [];
    }
}
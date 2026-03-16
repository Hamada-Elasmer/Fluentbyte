using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Engine.Runner;

public sealed class RunnerLog
{
    private readonly MLogger _log;
    private readonly int _sessionId;
    private readonly string _runId;

    public RunnerLog(MLogger log, int sessionId, string runId)
    {
        _log = log;
        _sessionId = sessionId;
        _runId = runId;
    }

    public void Info(string msg, string? profileId = null, params (string k, object? v)[] fields)
        => Write(LogLevel.INFO, msg, profileId, fields);

    public void Warn(string msg, string? profileId = null, params (string k, object? v)[] fields)
        => Write(LogLevel.WARNING, msg, profileId, fields);

    public void Error(Exception ex, string ctx, string? profileId = null, params (string k, object? v)[] fields)
    {
        var suffix = Format(fields);
        _log.Exception(LogComponent.Runner, LogChannel.SYSTEM, ex, $"{ctx}{suffix}", _sessionId, _runId, profileId);
    }

    private void Write(LogLevel level, string msg, string? profileId, params (string k, object? v)[] fields)
    {
        var suffix = Format(fields);
        _log.Log(LogComponent.Runner, LogChannel.SYSTEM, level, $"{msg}{suffix}", _sessionId, _runId, profileId);
    }

    private static string Format((string k, object? v)[] fields)
    {
        if (fields is null || fields.Length == 0) return "";
        // simple key=value suffix (safe for your current logger)
        return " | " + string.Join(" ", fields.Select(f => $"{f.k}={f.v}"));
    }
}
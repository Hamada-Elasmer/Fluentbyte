/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Logging/LogHub.cs
 * Purpose: Core component: LogHub.
 * Notes:
 *  - Subscribes to MLogger.LogEvent once and keeps an in-memory buffer for UI.
 *  - UI marshaling is handled using a captured SynchronizationContext.
 *  - Limits growth via maxLogs to prevent memory bloat.
 * ============================================================================ */

using System.Collections.ObjectModel;
using SparkFlow.Abstractions.Services.Logging;
using UtiliLib;
using UtiliLib.Events;
using UtiliLib.Models;

namespace SparkFlow.Infrastructure.Services.Logging;

public sealed class LogHub : ILogHub, IDisposable
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    private readonly SynchronizationContext? _uiCtx;
    private readonly int _maxLogs;
    private bool _disposed;

    public LogHub(int maxLogs = 3000, SynchronizationContext? uiContext = null)
    {
        _maxLogs = maxLogs <= 0 ? 3000 : maxLogs;

        // Capture UI context (App should create this on UI thread)
        _uiCtx = uiContext ?? SynchronizationContext.Current;

        // Subscribe once for whole app lifetime
        MLogger.Instance.LogEvent += OnLogEvent;
    }

    public void Clear()
        => PostToUi(() => Entries.Clear());

    private void OnLogEvent(object? sender, LogEventArgs e)
    {
        if (_disposed) return;

        var entry = e.LogEntry;
        PostToUi(() =>
        {
            Entries.Add(entry);

            // Trim overflow efficiently
            var overflow = Entries.Count - _maxLogs;
            if (overflow > 0)
            {
                for (var i = 0; i < overflow; i++)
                    Entries.RemoveAt(0);
            }
        });
    }

    private void PostToUi(Action action)
    {
        if (_disposed) return;

        // If we have UI context, always marshal to it
        if (_uiCtx != null)
        {
            _uiCtx.Post(_ =>
            {
                if (_disposed) return;
                try { action(); } catch { /* swallow */ }
            }, null);
            return;
        }

        // Fallback (prevents crash if context is missing)
        try { action(); } catch { /* swallow */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { MLogger.Instance.LogEvent -= OnLogEvent; } catch { /* ignore */ }
    }
}
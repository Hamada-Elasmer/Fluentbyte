/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/MLogger.cs
 * Purpose: Library component: MLogger.
 * Notes:
 *  - Component-based log routing (per-component daily files).
 *  - Dual output format:
 *      1) Console/UI (short, user-friendly):
 *         [TAG] (M/d/yyyy h:mm:ss tt) - Message
 *
 *      2) Files (rich, maintenance-friendly):
 *         yyyy-MM-dd HH:mm:ss.fff zzz [TAG] [C:..] [Ch:..] [Src:..] [Run:..] [Sess:..] [Profile:..] - Message
 *
 *  - Tag rules:
 *      - SYSTEM channel        => [SYSTEM]
 *      - NOTIFICATION channel  => [NOTIFICATION]
 *      - otherwise             => [INFO|WARNING|ERROR|DEBUG|EXCEPTION]
 *
 *  - UI debug policy:
 *      - Debug UI logs are represented as: Channel=UI + Level=DEBUG
 *      - Controlled by UiDebugEnabled.
 *
 * ============================================================================ */

using Serilog;
using Serilog.Core;
using Serilog.Events;
using UtiliLib.Events;
using UtiliLib.Logging;
using UtiliLib.Models;
using UtiliLib.Types;

namespace UtiliLib;

public sealed class MLogger
{
    // ================================
    // Public Serilog loggers (legacy convenience)
    // ================================
    public ILogger UiLog { get; private set; } = Serilog.Log.Logger;
    public ILogger ScriptLog { get; private set; } = Serilog.Log.Logger;
    public ILogger MainLog { get; private set; } = Serilog.Log.Logger;

    // ================================
    // UI Debug switch
    // ================================
    public bool UiDebugEnabled { get; set; } = false;

    // ================================
    // Singleton
    // ================================
    private static readonly Lazy<MLogger> InstanceLazy = new(() => new MLogger());
    public static MLogger Instance => InstanceLazy.Value;

    private bool _initialized;

    private MLogger() { }

    // ================================
    // Events (UI consumption)
    // ================================
    public event LoggedEventHandler? LogEvent;

    private void RaiseLogEvent(LogEntry entry)
        => LogEvent?.Invoke(this, new LogEventArgs(entry));

    // ================================
    // Init (component-based files + dual formatting)
    // ================================
    public void Init()
    {
        if (_initialized) return;
        _initialized = true;

        var baseDir = AppContext.BaseDirectory;
        var logsDir = Path.Combine(baseDir, "runtime", "logs");
        Directory.CreateDirectory(logsDir);

        // --------------------------------
        // Output templates
        // --------------------------------

        // ✅ Console/UI (short, matches your screenshot style)
        const string ConsoleTemplate =
            "[{Tag}] ({Timestamp:M/d/yyyy h:mm:ss tt}) - {Message:lj}{NewLine}{Exception}";

        // ✅ Files (rich context for development & maintenance)
        const string FileTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Tag}] " +
            "[C:{Component}] [Ch:{Channel}] [Src:{Source}] [Run:{RunId}] [Sess:{SessionId}] [Profile:{ProfileId}] - " +
            "{Message:lj}{NewLine}{Exception}";

        // --------------------------------
        // Base configuration
        // --------------------------------
        var baseConfig = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            // Ensures {Tag} is ALWAYS present in templates.
            .Enrich.With(new TagEnricher());

#if DEBUG
        // Debug window output uses the short console format
        baseConfig = baseConfig.WriteTo.Debug(outputTemplate: ConsoleTemplate);
#endif

        // --------------------------------
        // Helper: per-component routing (daily rolling files)
        // --------------------------------
        LoggerConfiguration AddComponentFile(LoggerConfiguration cfg, string filePrefix, string componentValue)
        {
            return cfg.WriteTo.Logger(lc =>
                lc.Filter.ByIncludingOnly(e =>
                    e.Properties.TryGetValue("Component", out var c) &&
                    string.Equals(c.ToString().Trim('"'), componentValue, StringComparison.OrdinalIgnoreCase))
                  .WriteTo.File(
                      path: Path.Combine(logsDir, $"{filePrefix}-.log"),
                      rollingInterval: RollingInterval.Day,
                      outputTemplate: FileTemplate,
                      shared: true,
                      retainedFileCountLimit: 14));
        }

        var config = baseConfig;

        // ✅ Per-component daily files (routing based on "Component" property)
        config = AddComponentFile(config, "system", nameof(LogComponent.System));
        config = AddComponentFile(config, "api", nameof(LogComponent.Api));
        config = AddComponentFile(config, "runner", nameof(LogComponent.Runner));
        config = AddComponentFile(config, "adb", nameof(LogComponent.Adb));
        config = AddComponentFile(config, "emulator", nameof(LogComponent.Emulator));
        config = AddComponentFile(config, "health", nameof(LogComponent.Health));
        config = AddComponentFile(config, "ui", nameof(LogComponent.Ui));
        config = AddComponentFile(config, "game", nameof(LogComponent.Game));
        config = AddComponentFile(config, "network", nameof(LogComponent.Network));
        config = AddComponentFile(config, "notification", nameof(LogComponent.Notification));
        config = AddComponentFile(config, "script", nameof(LogComponent.Script));
        config = AddComponentFile(config, "hint", nameof(LogComponent.Hint));
        config = AddComponentFile(config, "unknown", nameof(LogComponent.Unknown));

        // ✅ Catch-all (short retention)
        config = config.WriteTo.File(
            path: Path.Combine(logsDir, "sparkflow-all-.log"),
            rollingInterval: RollingInterval.Day,
            outputTemplate: FileTemplate,
            shared: true,
            retainedFileCountLimit: 7);

        // Build logger
        MainLog = config.CreateLogger();
        Serilog.Log.Logger = MainLog;

        // --------------------------------
        // Context loggers (still ok for quick usage)
        // --------------------------------
        UiLog = MainLog
            .ForContext("Channel", nameof(LogChannel.UI))
            .ForContext("Component", nameof(LogComponent.Ui))
            .ForContext("Source", "UI")
            .ForContext("AppLevel", nameof(LogLevel.INFO));

        ScriptLog = MainLog
            .ForContext("Channel", nameof(LogChannel.SCRIPT))
            .ForContext("Component", nameof(LogComponent.Script))
            .ForContext("Source", "Script")
            .ForContext("AppLevel", nameof(LogLevel.INFO));
    }

    // ==========================================================
    // Public API (legacy) - unchanged signatures
    // ==========================================================
    public void Log(LogChannel channel, LogLevel level, string message, int sessionId = 0)
        => Log(channel, level, message, sessionId, runId: null, profileId: null);

    public void Info(LogChannel channel, string message, int sessionId = 0)
        => Log(channel, LogLevel.INFO, message, sessionId);

    public void Warn(LogChannel channel, string message, int sessionId = 0)
        => Log(channel, LogLevel.WARNING, message, sessionId);

    public void Error(LogChannel channel, string message, int sessionId = 0)
        => Log(channel, LogLevel.ERROR, message, sessionId);

    public void Debug(LogChannel channel, string message, int sessionId = 0)
        => Log(channel, LogLevel.DEBUG, message, sessionId);

    /// <summary>
    /// UI debug is represented as: Channel=UI + Level=DEBUG
    /// </summary>
    public void UiDebug(string message, int sessionId = 0)
        => Log(LogChannel.UI, LogLevel.DEBUG, message, sessionId);

    public void Exception(LogChannel channel, Exception ex, string context = "", int sessionId = 0)
        => Exception(channel, ex, context, sessionId, runId: null, profileId: null);

    // ==========================================================
    // Public API (new) - explicit component override
    // ==========================================================
    public void Log(
        LogComponent component,
        LogChannel channel,
        LogLevel level,
        string message,
        int sessionId = 0,
        string? runId = null,
        string? profileId = null)
    {
        // UI debug policy: block noisy UI debug logs if disabled
        if (channel == LogChannel.UI && level == LogLevel.DEBUG && !UiDebugEnabled)
            return;

        var entry = new LogEntry
        {
            Component = component,
            Channel = channel,
            Level = level,
            Text = message ?? string.Empty,
            SessionId = sessionId,
            RunId = runId?.Trim() ?? string.Empty,
            ProfileId = profileId?.Trim() ?? string.Empty,

            // Default: show component name as Source unless overridden elsewhere
            Source = component.ToString()
        };

        InjectContext(entry);

        WriteToSerilog(entry);
        RaiseLogEvent(entry);
    }

    // ==========================================================
    // Public API (new) - with RunId/ProfileId context (kept)
    // ==========================================================
    public void Log(LogChannel channel, LogLevel level, string message, int sessionId, string? runId, string? profileId)
    {
        // UI debug policy: block noisy UI debug logs if disabled
        if (channel == LogChannel.UI && level == LogLevel.DEBUG && !UiDebugEnabled)
            return;

        var entry = new LogEntry
        {
            Channel = channel,
            Level = level,
            Text = message ?? string.Empty,
            SessionId = sessionId,
            RunId = runId?.Trim() ?? string.Empty,
            ProfileId = profileId?.Trim() ?? string.Empty
        };

        InjectContext(entry);

        // Default component mapping (Channel -> Component)
        if (entry.Component == LogComponent.System && channel != LogChannel.SYSTEM)
            entry.Component = MapComponent(channel);

        // Default Source (human label)
        if (string.IsNullOrWhiteSpace(entry.Source))
            entry.Source = entry.Component.ToString();

        WriteToSerilog(entry);
        RaiseLogEvent(entry);
    }

    // ==========================================================
    // Exceptions
    // ==========================================================
    public void Exception(
        LogComponent component,
        LogChannel channel,
        Exception ex,
        string context = "",
        int sessionId = 0,
        string? runId = null,
        string? profileId = null)
    {
        if (ex is null)
        {
            Log(component, channel, LogLevel.EXCEPTION,
                string.IsNullOrWhiteSpace(context) ? "Unknown exception" : context,
                sessionId, runId, profileId);
            return;
        }

        var exName = ex.GetType().Name;
        var msg = ex.Message ?? string.Empty;

        var text = string.IsNullOrWhiteSpace(context)
            ? $"{exName}: {msg}"
            : $"{context} | {exName}: {msg}";

        // Add first stack frame (human-friendly)
        string? firstStack = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                var lines = ex.StackTrace.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (lines.Length > 0) firstStack = lines[0];
            }
        }
        catch { /* ignore */ }

        if (!string.IsNullOrWhiteSpace(firstStack))
            text += $" | at {firstStack}";

        var entry = new LogEntry
        {
            Component = component,
            Channel = channel,
            Level = LogLevel.EXCEPTION,
            Text = text,
            SessionId = sessionId,
            RunId = runId?.Trim() ?? string.Empty,
            ProfileId = profileId?.Trim() ?? string.Empty,
            Source = component.ToString()
        };

        InjectContext(entry);

        var logger = BuildStructuredLogger(entry);
        logger.Error(ex, "{Message}", entry.Text);

        RaiseLogEvent(entry);
    }

    public void Exception(LogChannel channel, Exception ex, string context, int sessionId, string? runId, string? profileId)
        => Exception(MapComponent(channel), channel, ex, context, sessionId, runId, profileId);

    // ================================
    // Internal: Serilog write
    // ================================
    private void WriteToSerilog(LogEntry entry)
    {
        if (entry.Component == LogComponent.System && entry.Channel != LogChannel.SYSTEM)
            entry.Component = MapComponent(entry.Channel);

        if (string.IsNullOrWhiteSpace(entry.Source))
            entry.Source = entry.Component.ToString();

        var logger = BuildStructuredLogger(entry);

        switch (entry.Level)
        {
            case LogLevel.DEBUG:
                logger.Debug("{Message}", entry.Text);
                break;

            case LogLevel.WARNING:
                logger.Warning("{Message}", entry.Text);
                break;

            case LogLevel.ERROR:
                logger.Error("{Message}", entry.Text);
                break;

            case LogLevel.EXCEPTION:
                logger.Error("{Message}", entry.Text);
                break;

            default:
                logger.Information("{Message}", entry.Text);
                break;
        }
    }

    /// <summary>
    /// Builds a structured logger with stable context fields.
    /// TagEnricher uses Channel + AppLevel to compute {Tag}.
    /// </summary>
    private ILogger BuildStructuredLogger(LogEntry entry)
    {
        return MainLog
            .ForContext("Component", entry.Component.ToString())
            .ForContext("Channel", entry.Channel.ToString())
            .ForContext("Source", entry.Source ?? "Main")
            .ForContext("SessionId", entry.SessionId)
            .ForContext("RunId", entry.RunId ?? string.Empty)
            .ForContext("ProfileId", entry.ProfileId ?? string.Empty)
            // App-level severity (INFO/WARNING/ERROR/DEBUG/EXCEPTION)
            .ForContext("AppLevel", entry.Level.ToString());
    }

    private static LogComponent MapComponent(LogChannel channel)
    {
        return channel switch
        {
            LogChannel.SYSTEM => LogComponent.System,
            LogChannel.NETWORK => LogComponent.Network,
            LogChannel.NOTIFICATION => LogComponent.Notification,
            LogChannel.UI => LogComponent.Ui,
            LogChannel.SCRIPT => LogComponent.Script,
            LogChannel.HINT => LogComponent.Hint,
            LogChannel.GAME => LogComponent.Game,
            _ => LogComponent.Unknown
        };
    }

    // ================================
    // Context injection
    // ================================
    private static void InjectContext(LogEntry entry)
    {
        var ctx = LogContext.Current;
        if (ctx == null) return;

        if (string.IsNullOrWhiteSpace(entry.RunId) && !string.IsNullOrWhiteSpace(ctx.RunId))
            entry.RunId = ctx.RunId!.Trim();

        if (string.IsNullOrWhiteSpace(entry.ProfileId) && !string.IsNullOrWhiteSpace(ctx.ProfileId))
            entry.ProfileId = ctx.ProfileId!.Trim();
    }

    // ================================
    // Serilog Enricher: Tag computation
    // ================================
    private sealed class TagEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.ContainsKey("Tag"))
                return;

            var channel = ReadString(logEvent, "Channel");

            // Rule: SYSTEM / NOTIFICATION override everything
            if (string.Equals(channel, nameof(LogChannel.SYSTEM), StringComparison.OrdinalIgnoreCase))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Tag", "SYSTEM"));
                return;
            }

            if (string.Equals(channel, nameof(LogChannel.NOTIFICATION), StringComparison.OrdinalIgnoreCase))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Tag", "NOTIFICATION"));
                return;
            }

            // Prefer AppLevel (INFO/WARNING/ERROR/DEBUG/EXCEPTION)
            var appLevel = ReadString(logEvent, "AppLevel");
            if (!string.IsNullOrWhiteSpace(appLevel))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Tag", appLevel));
                return;
            }

            // Fallback: map Serilog level to friendly label
            var tag = logEvent.Level switch
            {
                LogEventLevel.Verbose => "DEBUG",
                LogEventLevel.Debug => "DEBUG",
                LogEventLevel.Information => "INFO",
                LogEventLevel.Warning => "WARNING",
                LogEventLevel.Error => "ERROR",
                LogEventLevel.Fatal => "ERROR",
                _ => "INFO"
            };

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Tag", tag));
        }

        private static string? ReadString(LogEvent e, string propertyName)
        {
            if (!e.Properties.TryGetValue(propertyName, out var v))
                return null;

            if (v is ScalarValue sv && sv.Value is string s)
                return s;

            return v.ToString().Trim('"');
        }
    }

    // ================================
    // Delegate
    // ================================
    public delegate void LoggedEventHandler(object sender, LogEventArgs e);
}
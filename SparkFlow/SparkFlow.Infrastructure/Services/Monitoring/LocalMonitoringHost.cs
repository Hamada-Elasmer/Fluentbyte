/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Monitoring/LocalMonitoringHost.cs
 * Purpose: Local Monitoring API host (Kestrel on localhost only).
 * Notes:
 *  - Binds only to 127.0.0.1 (local machine).
 *  - Component-based logging (Api / Network).
 * ============================================================================ */

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Logging;
using UtiliLib;
using UtiliLib.Abstractions;
using UtiliLib.Models;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Monitoring;

public enum LocalApiHostState
{
    Created,
    Loading,
    Listening,
    Failed,
    Stopped
}

public sealed class LocalMonitoringHost
{
    private readonly IPortScanner _ports;
    private readonly IGlobalRunnerService _runner;
    private readonly ILogHub _logHub;
    private readonly IProfilesStore _profiles;
    private readonly MLogger _log;

    private readonly object _sync = new();

    private WebApplication? _app;
    private Task? _runTask;

    private LocalApiHostState _state = LocalApiHostState.Created;
    public LocalApiHostState State => _state;

    public event Action<LocalApiHostState, LocalApiHostState>? StateChanged;

    public int Port { get; private set; }

    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    public LocalMonitoringHost(
        IPortScanner ports,
        IGlobalRunnerService runner,
        ILogHub logHub,
        IProfilesStore profiles,
        MLogger logger)
    {
        _ports = ports ?? throw new ArgumentNullException(nameof(ports));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _logHub = logHub ?? throw new ArgumentNullException(nameof(logHub));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _log = logger ?? MLogger.Instance;
    }

    // =========================================================
    // Start
    // =========================================================

    public Task StartAsync(int preferredPort = 5508, CancellationToken ct = default)
    {
        lock (_sync)
        {
            if (_runTask is not null && !_runTask.IsCompleted)
                return _runTask;

            Port = ResolvePort(preferredPort);

            if (Port <= 0)
            {
                SetState(LocalApiHostState.Failed);
                return Task.CompletedTask;
            }

            SetState(LocalApiHostState.Loading);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = "SparkFlow.LocalMonitoring",
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.WebHost.UseUrls($"http://127.0.0.1:{Port}");

            var app = builder.Build();

            ConfigureMiddleware(app);
            ConfigureEndpoints(app);

            _app = app;

            _runTask = Task.Run(async () =>
            {
                try
                {
                    await app.StartAsync(ct).ConfigureAwait(false);
                    SetState(LocalApiHostState.Listening);
                    await app.WaitForShutdownAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // expected on shutdown
                }
                catch (Exception ex)
                {
                    SetState(LocalApiHostState.Failed);
                    _log.Exception(LogComponent.Api, LogChannel.SYSTEM, ex, "[LocalApi] Host faulted");
                }
                finally
                {
                    SetState(LocalApiHostState.Stopped);
                }
            }, CancellationToken.None);

            return _runTask;
        }
    }

    // =========================================================
    // Stop
    // =========================================================

    public async Task StopAsync()
    {
        WebApplication? app;
        lock (_sync) app = _app;

        if (app is null)
            return;

        try
        {
            await app.StopAsync().ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
            // best effort
        }
        finally
        {
            lock (_sync) _app = null;
            SetState(LocalApiHostState.Stopped);
        }
    }

    // =========================================================
    // Middleware
    // =========================================================

    private void ConfigureMiddleware(WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var sw = Stopwatch.StartNew();
            var reqId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                await next().ConfigureAwait(false);
            }
            finally
            {
                sw.Stop();

                var path = ctx.Request.Path.HasValue
                    ? ctx.Request.Path.Value!
                    : "/";

                if (ShouldLogPath(path))
                {
                    var method = ctx.Request.Method;
                    var status = ctx.Response.StatusCode;
                    var ms = sw.ElapsedMilliseconds;

                    var level =
                        status >= 500 ? LogLevel.ERROR :
                        status >= 400 ? LogLevel.WARNING :
                        LogLevel.INFO;

                    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    var message =
                        $"HTTP {method} {path} {status} {ms}ms | rid={reqId} | ip={ip}";

                    _log.Log(LogComponent.Api, LogChannel.NETWORK, level, message);
                }
            }
        });
    }

    private static bool ShouldLogPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return
            path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // Endpoints
    // =========================================================

    private void ConfigureEndpoints(WebApplication app)
    {
        app.MapGet("/", () =>
            Results.Text("SparkFlow Local Monitoring API", "text/plain"));

        app.MapGet("/api/status", () =>
        {
            var uptime = DateTimeOffset.UtcNow - _startedAt;

            return Results.Json(new
            {
                app = "SparkFlow",
                state = State.ToString(),
                runnerState = _runner.State.ToString(),
                port = Port,
                uptimeSeconds = (long)uptime.TotalSeconds
            });
        });

        app.MapGet("/api/runner", () =>
            Results.Json(new { state = _runner.State.ToString() }));

        app.MapGet("/api/logs/latest", (int? take) =>
        {
            var n = take.GetValueOrDefault(200);
            if (n <= 0) n = 200;
            if (n > 2000) n = 2000;

            LogEntry[] snapshot;
            try
            {
                snapshot = _logHub.Entries.ToArray();
            }
            catch
            {
                snapshot = Array.Empty<LogEntry>();
            }

            var start = Math.Max(0, snapshot.Length - n);
            var slice = snapshot.Skip(start).ToArray();

            return Results.Json(new
            {
                take = n,
                count = slice.Length,
                items = slice
            });
        });

        app.MapGet("/api/profiles", async (CancellationToken ct) =>
        {
            var all = await _profiles.LoadAllAsync(ct).ConfigureAwait(false);

            var items = all.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                active = p.Active,
                instanceId = p.InstanceId,
                adbSerial = p.AdbSerial
            });

            return Results.Json(new
            {
                count = all.Count,
                items
            });
        });
    }

    // =========================================================
    // Helpers
    // =========================================================

    private int ResolvePort(int preferred)
    {
        if (!_ports.IsPortInUse(preferred))
            return preferred;

        return _ports.FindFreePort(preferred, 65000);
    }

    private void SetState(LocalApiHostState newState)
    {
        var old = _state;
        if (old == newState)
            return;

        _state = newState;

        _log.Log(LogComponent.Api, LogChannel.SYSTEM, LogLevel.DEBUG,
            $"Webserver State changed new state: {newState} - old state: {old}");

        StateChanged?.Invoke(newState, old);
    }
}

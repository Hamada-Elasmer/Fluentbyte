/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Utils/ConsoleRedirector.cs
 * Purpose: UI component: ConsoleRedirector.
 * Notes:
 *  - Redirects Console.Out/Console.Error into MLogger.
 *  - Registers global exception handlers and routes them to MLogger.
 *  - Prevents duplicate handler registration if EnableLogsOnly is called multiple times.
 * ============================================================================ */

using System;
using System.Threading.Tasks;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.Utils;

public static class ConsoleRedirector
{
    private static bool _enabled;

    /// <summary>
    /// Redirects Console output to MLogger using a single channel.
    /// - Console.Out  => INFO
    /// - Console.Error => ERROR
    /// Also attaches global exception handlers (once).
    /// </summary>
    public static void EnableLogsOnly(LogChannel channel)
    {
        // Redirect standard output streams
        Console.SetOut(new ConsoleToMLoggerWriter(channel, LogLevel.INFO));
        Console.SetError(new ConsoleToMLoggerWriter(channel, LogLevel.ERROR));

        // Prevent duplicate event subscriptions
        if (_enabled) return;
        _enabled = true;

        // Unhandled exceptions (AppDomain)
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception ?? new Exception("Unhandled exception");
                MLogger.Instance.Exception(channel, ex, "Unhandled exception");
            }
            catch
            {
                // swallow (never crash due to logging)
            }
        };

        // Unobserved Task exceptions (TaskScheduler)
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            try
            {
                MLogger.Instance.Exception(channel, e.Exception, "Unobserved task exception");
            }
            catch
            {
                // swallow (never crash due to logging)
            }
        };
    }
}
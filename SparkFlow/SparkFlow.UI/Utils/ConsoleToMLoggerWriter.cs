/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.UI/Utils/ConsoleToMLoggerWriter.cs
 * Purpose: UI component: ConsoleToMLoggerWriter.
 * Notes:
 *  - Redirects Console output into MLogger with a given channel/level.
 *  - For UI debug logs: use Channel=UI + Level=DEBUG (UI_DEBUG is deprecated).
 * ============================================================================ */

using System;
using System.IO;
using System.Text;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.UI.Utils;

public sealed class ConsoleToMLoggerWriter : TextWriter
{
    private readonly LogChannel _channel;
    private readonly LogLevel _level;

    private readonly StringBuilder _buffer = new();
    private bool _isReentrant;

    public ConsoleToMLoggerWriter(LogChannel channel, LogLevel level)
    {
        _channel = channel;
        _level = level;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            FlushLine();
            return;
        }

        if (value != '\r')
            _buffer.Append(value);
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        foreach (var ch in value)
            Write(ch);
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        FlushLine();
    }

    private void FlushLine()
    {
        if (_isReentrant)
        {
            _buffer.Clear();
            return;
        }

        var line = _buffer.ToString();
        _buffer.Clear();

        if (string.IsNullOrWhiteSpace(line))
            return;

        try
        {
            _isReentrant = true;

            switch (_level)
            {
                case LogLevel.ERROR:
                    MLogger.Instance.Error(_channel, line);
                    break;

                case LogLevel.WARNING:
                    MLogger.Instance.Warn(_channel, line);
                    break;

                case LogLevel.DEBUG:
                    MLogger.Instance.Debug(_channel, line);
                    break;

                case LogLevel.EXCEPTION:
                    MLogger.Instance.Exception(_channel, new Exception(line), "Console exception");
                    break;

                default:
                    MLogger.Instance.Info(_channel, line);
                    break;
            }
        }
        finally
        {
            _isReentrant = false;
        }
    }
}
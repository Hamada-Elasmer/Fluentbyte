/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Services/AdbProvisioning.cs
 * Purpose: Library component: AdbProvisioning.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */


using AdbLib.Abstractions;
using AdbLib.Exceptions;
using AdbLib.Internal;
using AdbLib.Options;
using System;
using System.IO;
using UtiliLib;
using UtiliLib.Abstractions;
using UtiliLib.Helpers;
using UtiliLib.Infrastructure.Windows;
using UtiliLib.Options;
using UtiliLib.Types;

namespace AdbLib.Services;

public sealed class AdbProvisioning : IAdbProvisioning
{
    private const int AdbServerPort = 5037;

    private readonly AdbOptions _opt;
    private readonly AdbPaths _paths;
private readonly IPortScanner _scanner;
    private readonly PortScannerOptions _scanOpt;

    public AdbProvisioning(
        AdbOptions options,
        IPortScanner scanner,
        PortScannerOptions scanOptions)
    {
        _opt = options ?? throw new ArgumentNullException(nameof(options));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _scanOpt = scanOptions ?? new PortScannerOptions();

        _paths = new AdbPaths
        {
            BundledDir = _opt.BundledPlatformToolsDir,
            RuntimeDir = _opt.RuntimePlatformToolsDir,
            AdbExeName = _opt.AdbExeName,
            VersionFileName = _opt.VersionFileName
        };
}

    public string EnsureProvisioned()
    {
        ValidateBundledDir();
        
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.RuntimeDir) ?? _paths.RuntimeDir);

        var installedVersion = TryReadText(_paths.RuntimeVersionFile);
        var adbExists = File.Exists(_paths.RuntimeAdbExe);

        var needsInstall =
            !Directory.Exists(_paths.RuntimeDir) ||
            !adbExists ||
            string.IsNullOrWhiteSpace(installedVersion) ||
            !string.Equals(installedVersion.Trim(), _opt.BundledVersion, StringComparison.OrdinalIgnoreCase);

        if (!needsInstall)
        {
            GuardAdbServerPortOrThrow();
            if (_opt.RestartServerOnProvision) SafeRestartServer();
            GuardAdbServerPortOrThrow();
            return _paths.RuntimeAdbExe;
        }

        SafeKillServerPreferred();

        var runtimeDir = _paths.RuntimeDir;
        var backupDir = runtimeDir + "_old";
        var tempDir = runtimeDir + "_tmp";

        TryDeleteDirectory(backupDir);
        TryDeleteDirectory(tempDir);

        try
        {
            CopyDirectory(_paths.BundledDir, tempDir);

            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, _opt.VersionFileName), _opt.BundledVersion);

            if (Directory.Exists(runtimeDir))
                TryMoveDirectory(runtimeDir, backupDir);

            Directory.Move(tempDir, runtimeDir);

            TryDeleteDirectory(backupDir);

            GuardAdbServerPortOrThrow();
            if (_opt.RestartServerOnProvision) SafeRestartServer();
            GuardAdbServerPortOrThrow();

            return _paths.RuntimeAdbExe;
        }
        catch (Exception ex)
        {
            try
            {
                if (!Directory.Exists(runtimeDir) && Directory.Exists(backupDir))
                    Directory.Move(backupDir, runtimeDir);
            }
            catch { }

            throw new AdbProvisioningException("Failed to provision ADB platform-tools into runtime.", ex);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    // =========================
    // Port guard policy
    // =========================
    private void GuardAdbServerPortOrThrow()
    {
        for (int attempt = 0; attempt <= _scanOpt.MaxRetries; attempt++)
        {
            var (ok, detail) = _scanner.TryGetPortDetail(AdbServerPort);

            if (!ok) return;                 // free
            if (IsAdbProcess(detail)) return; // adb owns it -> ok

            MLogger.Instance.Warn(LogChannel.SYSTEM,
                $"[ADB] Port {AdbServerPort} occupied by '{detail.ProcessName}' (PID {detail.ProcessId}) attempt={attempt}/{_scanOpt.MaxRetries}");

            if (_scanOpt.AllowAggressiveReclaim && !LooksLikeSystemProcess(detail.ProcessName))
            {
                try
                {
                    MLogger.Instance.Warn(LogChannel.SYSTEM,
                        $"[ADB] AggressiveReclaim -> killing PID {detail.ProcessId} ({detail.ProcessName})");
                    WindowsService.KillOnlyProcess(detail.ProcessId).GetAwaiter().GetResult();
                }
                catch { }
            }

            if (attempt < _scanOpt.MaxRetries)
                System.Threading.Thread.Sleep(_scanOpt.RetryDelayMs);
        }

        var (_, last) = _scanner.TryGetPortDetail(AdbServerPort);
        throw new AdbProvisioningException(
            $"ADB server port {AdbServerPort} is occupied by '{last.ProcessName}' (PID {last.ProcessId}). Close it or enable AllowAggressiveReclaim.");
    }

    private static bool IsAdbProcess(UtiliLib.Helpers.PortDetail d)
        => (d.ProcessName ?? string.Empty).Contains("adb", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeSystemProcess(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim().ToLowerInvariant();
        return n is "system" or "svchost" or "services" or "wininit" or "csrss";
    }

    private void ValidateBundledDir()
    {
        if (!Directory.Exists(_paths.BundledDir))
            throw new AdbProvisioningException($"Bundled platform-tools directory not found: {_paths.BundledDir}");

        var bundledAdb = Path.Combine(_paths.BundledDir, _opt.AdbExeName);
        if (!File.Exists(bundledAdb))
            throw new AdbProvisioningException($"Bundled adb executable not found: {bundledAdb}");
    }

    private void SafeRestartServer()
    {
        var adbExe = File.Exists(_paths.RuntimeAdbExe)
            ? _paths.RuntimeAdbExe
            : Path.Combine(_paths.BundledDir, _opt.AdbExeName);

        var wd = Path.GetDirectoryName(adbExe)!;

        try { ProcessExec.Run(adbExe, new[] { "kill-server" }, wd, 12_000); } catch { }
        try { ProcessExec.Run(adbExe, new[] { "start-server" }, wd, 20_000); } catch { }
    }

    private void SafeKillServerPreferred()
    {
        var adbExe = File.Exists(_paths.RuntimeAdbExe)
            ? _paths.RuntimeAdbExe
            : Path.Combine(_paths.BundledDir, _opt.AdbExeName);

        var wd = Path.GetDirectoryName(adbExe)!;

        try { ProcessExec.Run(adbExe, new[] { "kill-server" }, wd, 12_000); } catch { }
    }

    private static string? TryReadText(string filePath)
    {
        try { return File.Exists(filePath) ? File.ReadAllText(filePath) : null; }
        catch { return null; }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        catch { }
    }

    private static void TryMoveDirectory(string src, string dest)
    {
        try
        {
            if (Directory.Exists(dest))
                TryDeleteDirectory(dest);

            Directory.Move(src, dest);
        }
        catch { }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
}
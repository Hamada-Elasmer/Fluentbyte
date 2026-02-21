/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/Utils/RuntimeAssetsSync.cs
 * Purpose: UI component: RuntimeAssetsSync.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.IO;

namespace SparkFlow.UI.Utils;

public static class RuntimeAssetsSync
{
    private const string MarkerFileName = ".platform-tools.sync.json";

    public static void EnsurePlatformTools(string baseDir)
    {
        var assetsDir = Path.Combine(baseDir, "Assets", "platform-tools");
        var runtimeDir = Path.Combine(baseDir, "runtime", "platform-tools");

        if (!Directory.Exists(assetsDir))
            return;

        Directory.CreateDirectory(runtimeDir);

        var markerPath = Path.Combine(runtimeDir, MarkerFileName);

        var manifest = BuildManifest(assetsDir);
        var last = TryReadManifest(markerPath);

        // already synced (same assets footprint)
        if (last != null && last.Fingerprint == manifest.Fingerprint)
            return;

        // sync: copy only missing files (never overwrite)
        CopyMissingOnly(assetsDir, runtimeDir);

        // write marker
        manifest.SyncedAtUtc = DateTime.UtcNow;
        TryWriteManifest(markerPath, manifest);
    }

    public static void ForceResync(string baseDir)
    {
        var runtimeDir = Path.Combine(baseDir, "runtime", "platform-tools");
        var markerPath = Path.Combine(runtimeDir, MarkerFileName);
        TryDelete(markerPath);

        EnsurePlatformTools(baseDir);
    }

    // =========================
    // internals
    // =========================

    private static void CopyMissingOnly(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(file));

            // never overwrite runtime files
            if (File.Exists(dest))
                continue;

            try
            {
                File.Copy(file, dest, overwrite: false);
            }
            catch
            {
                // ignore IO / access errors
            }
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyMissingOnly(dir, dest);
        }
    }

    private static SyncManifest BuildManifest(string assetsDir)
    {
        var files = Directory.Exists(assetsDir)
            ? Directory.GetFiles(assetsDir, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

        // stable ordering
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        // fingerprint = deterministic string of (relativePath|length|lastWriteUtcTicks)
        // (fast, no hashing, enough for "did assets change?")
        var parts = new List<string>(files.Length);

        foreach (var abs in files)
        {
            var rel = Path.GetRelativePath(assetsDir, abs).Replace('\\', '/');
            var fi = new FileInfo(abs);
            parts.Add($"{rel}|{fi.Length}|{fi.LastWriteTimeUtc.Ticks}");
        }

        var fingerprint = string.Join(";", parts);

        return new SyncManifest
        {
            AssetsRoot = assetsDir,
            FilesCount = files.Length,
            Fingerprint = fingerprint
        };
    }

    private static SyncManifest? TryReadManifest(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<SyncManifest>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void TryWriteManifest(string path, SyncManifest manifest)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }
        catch
        {
            // ignore
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }

    private sealed class SyncManifest
    {
        public string AssetsRoot { get; set; } = "";
        public int FilesCount { get; set; }
        public string Fingerprint { get; set; } = "";
        public DateTime? SyncedAtUtc { get; set; }
    }
}
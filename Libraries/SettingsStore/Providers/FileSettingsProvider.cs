using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SettingsStore.Interfaces;
using SettingsStore.Models;

namespace SettingsStore.Providers;

/// <summary>
/// JSON file-based settings provider.
/// Stores application preferences on disk with atomic writes and safe recovery.
/// </summary>
public sealed class FileSettingsProvider : ISettingsProviderAsync
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };

    public FileSettingsProvider(string filePath)
    {
        _filePath = filePath;
    }

    public AppSettings Load() => LoadAsync().GetAwaiter().GetResult();

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            EnsureFileExists();

            // Try primary file, then .bak if primary fails
            var primary = await TryReadSettingsFromFile(_filePath, ct).ConfigureAwait(false);
            if (primary != null) return primary;

            var bakPath = _filePath + ".bak";
            if (File.Exists(bakPath))
            {
                var bak = await TryReadSettingsFromFile(bakPath, ct).ConfigureAwait(false);
                if (bak != null)
                {
                    // Repair main file from backup (best-effort)
                    try { await SaveAsync(bak, ct).ConfigureAwait(false); } catch { /* best-effort */ }
                    return bak;
                }
            }

            // If everything fails, reset to defaults (and persist)
            BackupCorruptFile();
            var defaults = new AppSettings();
            await SaveAsync(defaults, ct).ConfigureAwait(false);
            return defaults;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Save(AppSettings settings) => SaveAsync(settings).GetAwaiter().GetResult();

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            // Wrap in document for schema versioning (explicit)
            var doc = new SettingsDocument
            {
                SchemaVersion = SettingsDocument.CurrentSchemaVersion,
                Settings = settings
            };

            var json = JsonConvert.SerializeObject(doc, SerializerSettings);

            // Atomic write: write temp then replace/move
            var tmp = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmp, json, Encoding.UTF8, ct).ConfigureAwait(false);

            if (File.Exists(_filePath))
            {
                var bak = _filePath + ".bak";
                try
                {
                    File.Replace(tmp, _filePath, bak, ignoreMetadataErrors: true);
                }
                catch
                {
                    // Fallback if Replace isn't supported
                    File.Copy(tmp, _filePath, overwrite: true);
                    File.Delete(tmp);
                }
            }
            else
            {
                File.Move(tmp, _filePath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public AppSettings Reset() => ResetAsync().GetAwaiter().GetResult();

    public async Task<AppSettings> ResetAsync(CancellationToken ct = default)
    {
        var settings = new AppSettings();
        await SaveAsync(settings, ct).ConfigureAwait(false);
        return settings;
    }

    // -------------------------
    // Internals
    // -------------------------

    private async Task<AppSettings?> TryReadSettingsFromFile(string path, CancellationToken ct)
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            // Support both formats:
            // 1) Legacy: AppSettings JSON
            // 2) Document: { schemaVersion, settings }
            var token = JToken.Parse(json);

            if (token is JObject obj && obj.Property("settings") != null)
            {
                var doc = obj.ToObject<SettingsDocument>();
                if (doc?.Settings == null) return null;

                var migrated = MigrateIfNeeded(doc);
                return migrated ?? new AppSettings();
            }

            var legacy = token.ToObject<AppSettings>();
            return legacy;
        }
        catch
        {
            // Parse failed
            return null;
        }
    }

    private static AppSettings? MigrateIfNeeded(SettingsDocument doc)
    {
        // Future-proof: if we bump schema versions later.
        // For now (v1) it's a no-op, but the hook is here.
        if (doc.SchemaVersion >= SettingsDocument.CurrentSchemaVersion)
            return doc.Settings;

        var s = doc.Settings ?? new AppSettings();

        // Example migration pattern (keep for when you bump versions):
        // if (doc.SchemaVersion == 0) { ... set new defaults ... }

        // Ensure schema is upgraded logically
        doc.SchemaVersion = SettingsDocument.CurrentSchemaVersion;
        return s;
    }

    private void EnsureFileExists()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(_filePath))
        {
            var doc = new SettingsDocument
            {
                SchemaVersion = SettingsDocument.CurrentSchemaVersion,
                Settings = new AppSettings()
            };
            var json = JsonConvert.SerializeObject(doc, SerializerSettings);
            File.WriteAllText(_filePath, json, Encoding.UTF8);
        }
    }

    private void BackupCorruptFile()
    {
        try
        {
            if (!File.Exists(_filePath)) return;

            var dir = Path.GetDirectoryName(_filePath) ?? ".";
            var name = Path.GetFileNameWithoutExtension(_filePath);
            var ext = Path.GetExtension(_filePath);
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var corrupt = Path.Combine(dir, $"{name}.corrupt_{ts}{ext}");
            File.Copy(_filePath, corrupt, overwrite: true);
        }
        catch
        {
            // best-effort
        }
    }
}
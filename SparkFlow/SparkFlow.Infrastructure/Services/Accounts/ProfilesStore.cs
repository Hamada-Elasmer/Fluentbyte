/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Accounts/ProfilesStore.cs
 * Purpose: Core component: ProfilesStore.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Text.Json;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Domain.Models.Accounts;

namespace SparkFlow.Infrastructure.Services.Accounts;

public sealed class ProfilesStore : IProfilesStore
{
    private readonly string _dir;

    public event Action<string>? ProfileChanged;

    private static readonly JsonSerializerOptions JsonOpt = new()
    {
        WriteIndented = true
    };

    public ProfilesStore()
    {
        _dir = Path.Combine(AppContext.BaseDirectory, "runtime", "profiles");
        Directory.CreateDirectory(_dir);
    }

    public async Task<IReadOnlyList<AccountProfile>> LoadAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!Directory.Exists(_dir))
            return Array.Empty<AccountProfile>();

        var files = Directory.GetFiles(_dir, "*.json", SearchOption.TopDirectoryOnly);
        var list = new List<AccountProfile>();

        foreach (var path in files)
        {
            ct.ThrowIfCancellationRequested();
            var p = await TryReadAsync(path, ct).ConfigureAwait(false);
            if (p != null) list.Add(p);
        }

        return list;
    }

    public Task<AccountProfile?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<AccountProfile?>(null);

        var path = Path.Combine(_dir, $"{id}.json");
        return TryReadAsync(path, ct);
    }

    public async Task SaveAsync(AccountProfile profile, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        if (string.IsNullOrWhiteSpace(profile.Id))
            throw new ArgumentException("Profile.Id is required.");

        Directory.CreateDirectory(_dir);

        var now = DateTimeOffset.Now;

        var normalized = profile.Clone();
        normalized.CreatedAt ??= now;
        normalized.UpdatedAt = now;

        var json = JsonSerializer.Serialize(normalized, JsonOpt);

        var path = Path.Combine(_dir, $"{normalized.Id}.json");
        var tmp = path + ".tmp";

        await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tmp, path);

        ProfileChanged?.Invoke(normalized.Id);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(id)) return Task.CompletedTask;

        var path = Path.Combine(_dir, $"{id}.json");

        if (File.Exists(path))
            File.Delete(path);

        ProfileChanged?.Invoke(id);

        return Task.CompletedTask;
    }

    private static async Task<AccountProfile?> TryReadAsync(string path, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);

            // ===============================
            // Direct deserialize (new format)
            // ===============================
            try
            {
                var obj = JsonSerializer.Deserialize<AccountProfile>(json);
                if (obj is not null &&
                    !string.IsNullOrWhiteSpace(obj.Id) &&
                    !string.IsNullOrWhiteSpace(obj.Name))
                {
                    // Ensure InstanceId is normalized (legacy may contain null/none)
                    obj.InstanceId = NormalizeInstanceId(obj.InstanceId);
                    return obj;
                }
            }
            catch
            {
                // ignore and fallback
            }

            // ===============================
            // Legacy fallback
            // ===============================
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var id = ReadString(root, "Id", null);
            var name = ReadString(root, "Name", null);

            // Legacy instance id might be number or string
            var instanceId = ReadString(root, "InstanceId", "-1");

            var active =
                ReadBool(root, "Active") ||
                ReadBool(root, "Enabled") ||
                ReadBool(root, "IsEnabled");

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                return null;

            return new AccountProfile
            {
                Id = id!.Trim(),
                Name = name!.Trim(),
                InstanceId = NormalizeInstanceId(instanceId),
                Active = active
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool ReadBool(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var v)) return false;

        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => v.TryGetInt32(out var n) && n != 0,
            JsonValueKind.String => bool.TryParse(v.GetString(), out var b) && b,
            _ => false
        };
    }

    private static string? ReadString(JsonElement root, string key, string? def)
    {
        if (!root.TryGetProperty(key, out var v)) return def;

        if (v.ValueKind == JsonValueKind.String)
            return v.GetString()?.Trim();

        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n))
            return n.ToString();

        return def;
    }

    private static string NormalizeInstanceId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "-1";

        value = value.Trim();

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            return "-1";

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
            return "-1";

        return value;
    }
}

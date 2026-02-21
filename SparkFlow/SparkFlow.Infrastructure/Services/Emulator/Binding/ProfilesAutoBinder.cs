/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Services/Emulator/Binding/ProfilesAutoBinder.cs
 * Purpose: Core component: ProfilesAutoBinder.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using AdbLib.Abstractions;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Emulator.Binding;
using SparkFlow.Infrastructure.Services.Accounts;
using UtiliLib;
using UtiliLib.Types;

namespace SparkFlow.Infrastructure.Services.Emulator.Binding;

/// <summary>
/// Strict auto-binder:
/// - ONLY binds profiles that have InstanceId (numeric, zero-based).
/// - Binds to the expected emulator serial derived from InstanceId:
///     instanceId=0 -> emulator-5554
///     instanceId=1 -> emulator-5556
///     instanceId=2 -> emulator-5558
/// - NEVER assigns a random free device to a profile.
/// - Devices must be online (adb state == "device").
/// </summary>
public sealed class ProfilesAutoBinder : IProfilesAutoBinder
{
    private readonly IProfilesStore _profiles;
    private readonly IAdbClient _adb;
    private readonly ProfileDeviceBinder _binder;
    private readonly MLogger _log;

    public ProfilesAutoBinder(
        IProfilesStore profilesStore,
        IAdbClient adb,
        ProfileDeviceBinder binder,
        MLogger logger)
    {
        _profiles = profilesStore ?? throw new ArgumentNullException(nameof(profilesStore));
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
        _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        _log = logger ?? MLogger.Instance;
    }

    public async Task AutoBindUnboundProfilesAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            // 1) Read online devices (ready only)
            var onlineSerials = _adb.Devices()
                .Where(d => string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Serial)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (onlineSerials.Count == 0)
                return;

            // 2) Load profiles
            var all = (await _profiles.LoadAllAsync(ct).ConfigureAwait(false)).ToList();
            if (all.Count == 0)
                return;

            // 3) Compute used serials (already bound)
            var used = all
                .Where(p => !string.IsNullOrWhiteSpace(p.AdbSerial))
                .Select(p => p.AdbSerial!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 4) Candidates: unbound profiles WITH valid InstanceId
            var candidates = all
                .Where(p => string.IsNullOrWhiteSpace(p.AdbSerial))
                .Where(p => HasValidInstanceId(p.InstanceId))
                .OrderBy(p => p.CreatedAt ?? DateTimeOffset.MinValue)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (candidates.Count == 0)
                return;

            var boundCount = 0;

            foreach (var p in candidates)
            {
                ct.ThrowIfCancellationRequested();

                var expected = DeriveExpectedSerialFromInstanceId(p.InstanceId);
                if (string.IsNullOrWhiteSpace(expected))
                    continue;

                // Must be online right now
                if (!onlineSerials.Contains(expected))
                    continue;

                // Must be free (not used by another profile)
                if (used.Contains(expected))
                    continue;

                // Bind to the expected serial only (STRICT)
                p.AdbSerial = expected; // guarantee never left null
                _binder.BindBestEffort(p, expected);

                await _profiles.SaveAsync(p, ct).ConfigureAwait(false);

                used.Add(expected);
                boundCount++;

                _log.Info(LogChannel.SYSTEM, $"[AutoBind] Bound '{p.Name}' | InstanceId={p.InstanceId} -> {expected}");
            }

            if (boundCount > 0)
                _log.Info(LogChannel.SYSTEM, $"[AutoBind] Completed. BoundProfiles={boundCount}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Warn(LogChannel.SYSTEM, $"[AutoBind] Failed: {ex.Message}");
        }
    }

    private static bool HasValidInstanceId(string? instanceId)
    {
        instanceId = string.IsNullOrWhiteSpace(instanceId) ? null : instanceId.Trim();
        if (instanceId is null) return false;
        if (instanceId == "-1") return false;

        return int.TryParse(instanceId, out var n) && n >= 0;
    }

    // Zero-based mapping:
    // 0 -> emulator-5554
    // 1 -> emulator-5556
    // 2 -> emulator-5558
    private static string? DeriveExpectedSerialFromInstanceId(string? instanceId)
    {
        instanceId = string.IsNullOrWhiteSpace(instanceId) ? null : instanceId.Trim();
        if (instanceId is null) return null;

        if (!int.TryParse(instanceId, out var n) || n < 0)
            return null;

        var port = 5554 + (n * 2);
        return $"emulator-{port}";
    }
}

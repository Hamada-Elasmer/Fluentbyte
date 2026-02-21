/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.UI/ViewModels/Dialogs/Contents/GameInfoDialogContentViewModel.cs
 * Purpose: UI component: GameInfoDialogContentViewModel.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Threading;
using System.Threading.Tasks;
using AdbLib.Abstractions;
using SettingsStore.Interfaces;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.UI.ViewModels.Shell;

namespace SparkFlow.UI.ViewModels.Dialogs.Contents;

/// <summary>
/// Read-only dialog view model that shows basic game information for a profile.
///
/// Policy:
/// - No install / update actions (display only).
/// - Uses the profile's explicit ADB serial (ADB-first).
/// - Shows only: Game Name + Game Version.
/// </summary>
public sealed class GameInfoDialogContentViewModel : ViewModelBase
{
    private const string WarAndOrderPackage = "com.camelgames.wo";
    private const string GameNameConst = "War and Order";

    private IAdbClient? _adb;
    private IProfilesStore? _profiles;
    private ISettingsAccessor? _settings;

    private string? _profileId;

    // ✅ NEW: Some dialogs/factories bind via property instead of calling ActivateAsync directly.
    private string _boundProfileId = "";

    private bool _isLoading;
    private string _gameName = GameNameConst;
    private string _gameVersion = "-";

    public string GameName
    {
        get => _gameName;
        private set => this.RaiseAndSetIfChanged(ref _gameName, value);
    }

    public string GameVersion
    {
        get => _gameVersion;
        private set => this.RaiseAndSetIfChanged(ref _gameVersion, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// ✅ NEW:
    /// Some UI paths set the profile through a bindable property (e.g. vm.BoundProfileId = profileId).
    /// This keeps compatibility with existing ActivateAsync(profileId) usage.
    /// </summary>
    public string BoundProfileId
    {
        get => _boundProfileId;
        set
        {
            var v = value?.Trim() ?? "";
            if (string.Equals(_boundProfileId, v, StringComparison.OrdinalIgnoreCase))
                return;

            this.RaiseAndSetIfChanged(ref _boundProfileId, v);

            // Keep legacy field in sync (if older code reads _profileId).
            _profileId = string.IsNullOrWhiteSpace(v) ? null : v;

            // Best-effort auto refresh when services are ready.
            // (Avoid throwing, never block UI thread.)
            if (_adb is not null && _profiles is not null && _settings is not null && !string.IsNullOrWhiteSpace(v))
            {
                _ = RefreshAsync(CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Called by the dialog service after constructing the dialog.
    /// </summary>
    public void SetServices(IAdbClient adb, IProfilesStore profiles, ISettingsAccessor settings)
    {
        _adb = adb ?? throw new ArgumentNullException(nameof(adb));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        // ✅ If profile was already bound before SetServices, refresh now.
        var pid = !string.IsNullOrWhiteSpace(_boundProfileId) ? _boundProfileId : _profileId;
        if (!string.IsNullOrWhiteSpace(pid))
        {
            _profileId = pid;
            _ = RefreshAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Activates the dialog for a given profile id.
    /// </summary>
    public async Task ActivateAsync(string profileId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile id is required.", nameof(profileId));

        _profileId = profileId.Trim();

        // ✅ Keep new property in sync as well (so both paths behave the same).
        if (!string.Equals(_boundProfileId, _profileId, StringComparison.OrdinalIgnoreCase))
            _boundProfileId = _profileId;

        await RefreshAsync(ct);
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        if (_adb is null || _profiles is null || _settings is null)
            throw new InvalidOperationException("Services are not initialized.");

        // Prefer BoundProfileId if present (new path), otherwise use legacy _profileId.
        var pid = !string.IsNullOrWhiteSpace(_boundProfileId) ? _boundProfileId : _profileId;

        if (string.IsNullOrWhiteSpace(pid))
        {
            GameName = GameNameConst;
            GameVersion = "Not available";
            return;
        }

        IsLoading = true;
        try
        {
            // Resolve the current ADB serial (profile is the source of truth).
            var profile = await _profiles.GetByIdAsync(pid);
            var serial = profile?.AdbSerial?.Trim();

            if (string.IsNullOrWhiteSpace(serial))
            {
                GameName = GameNameConst;
                GameVersion = "Not available";
                return;
            }

            ct.ThrowIfCancellationRequested();

            // Ensure device is ready before running package commands.
            await _adb.WaitForDeviceReadyAsync(serial, ct);

            // If the package isn't installed, keep the version field meaningful.
            var pmPath = _adb.Shell(serial, $"pm path {WarAndOrderPackage}", timeoutMs: 12_000);
            if (!LooksInstalledFromPmPath(pmPath))
            {
                GameName = GameNameConst;
                GameVersion = "Not installed";
                return;
            }

            // Extract versionName from dumpsys output.
            var dumpsys = _adb.Shell(serial, $"dumpsys package {WarAndOrderPackage}", timeoutMs: 12_000);
            var versionName = TryReadVersionName(dumpsys) ?? "Unknown";

            GameName = GameNameConst;
            GameVersion = versionName;
        }
        catch (Exception)
        {
            // Keep the dialog resilient: we still render, but show that data isn't available.
            GameName = GameNameConst;
            GameVersion = "Not available";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string? TryReadVersionName(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        // Common dumpsys formats:
        // - "versionName=1.2.3"
        // - "versionName=1.2.3.456"
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            var idx = line.IndexOf("versionName=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                continue;

            var v = line.Substring(idx + "versionName=".Length).Trim();
            if (string.IsNullOrWhiteSpace(v))
                return null;

            // Some outputs include extra tokens; keep only the first token.
            var sp = v.IndexOf(' ');
            if (sp > 0)
                v = v.Substring(0, sp).Trim();

            return v;
        }

        return null;
    }

    private static bool LooksInstalledFromPmPath(string outp)
    {
        if (string.IsNullOrWhiteSpace(outp))
            return false;

        foreach (var rawLine in outp.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("package:", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line.Substring("package:".Length).Trim();
            if (value.StartsWith("/", StringComparison.Ordinal) && value.Length > 1)
                return true;
        }

        return false;
    }
}

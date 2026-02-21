/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow/SparkFlow.UI/ViewModels/Pages/Accounts/AccountsPageViewModel.cs
 * Purpose: UI component: AccountsPageViewModel.
 * Notes:
 *  - Emulator-first discovery for onboarding.
 *  - Runtime actions remain ADB-first.
 *  - Binding resolve is progressive to avoid UI freezes.
 *  - No KillServer in UI refresh.
 * ============================================================================ 
 */

using AdbLib.Abstractions;
using AdbLib.Models;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.Input;

using EmulatorLib.Abstractions;

using GameModules.WarAndOrder.Ports;

using SparkFlow.UI.Services.Windows;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using SparkFlow.Abstractions.Abstractions;
using SparkFlow.Abstractions.Services.Accounts;
using SparkFlow.Abstractions.Services.Dialogs;
using SparkFlow.Abstractions.Services.Emulator.Binding;
using SparkFlow.Domain.Models.Accounts;
using SparkFlow.Infrastructure.Services.Accounts;
using SparkFlow.UI.ViewModels.Shell;
using UtiliLib;
using UtiliLib.Notifications;
using UtiliLib.Types;

namespace SparkFlow.UI.ViewModels.Pages.Accounts;

public sealed class AccountsPageViewModel : ViewModelBase, IActivatable
{
    private readonly IEmulator _emulator;

    // ✅ Core-owned abstraction (UI must NOT depend on EmulatorLib implementations)
    private readonly IPlatformRequirementGuard _platformGuard;

    private readonly IAdbClient _adb;

    private readonly IProfilesStore _profilesStore;
    private readonly ProfileDeviceBinder _profileBinder;
    private readonly ProfileDeviceResolver _profileResolver;

    private readonly IProfilesAutoBinder _autoBinder;

    private readonly IAccountsSelector _accountsSelector;
    private readonly IAccountWindowsService _accountWindows;
    private readonly IDialogService _dialogs;

    private readonly IDeviceAutomation _deviceAutomation;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public ObservableCollection<ProfileItemViewModel> Profiles { get; } = new();

    private ProfileItemViewModel? _selectedProfile;
    private bool _suppressSelectionSave;
    private bool _entered;

    // ==========================================================
    // ✅ Profiles change notifications (Core -> UI refresh)
    // ==========================================================
    private int _profileChangedScheduled;
    private bool _ignoreProfileChanged;

    // ==========================================================
    // Warning UI
    // ==========================================================
    private bool _showWarning;
    public bool ShowNoFreeInstancesWarning
    {
        get => _showWarning;
        private set => RaiseAndSetIfChanged(ref _showWarning, value);
    }

    private string _warningText = "No instances found.";
    public string WarningText
    {
        get => _warningText;
        private set => RaiseAndSetIfChanged(ref _warningText, value);
    }

    // ==========================================================
    // Selection
    // ==========================================================
    public ProfileItemViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (RaiseAndSetIfChanged(ref _selectedProfile, value))
            {
                if (_suppressSelectionSave)
                    return;

                try
                {
                    _accountsSelector.Select(value?.Id);
                    MLogger.Instance.Info(LogChannel.UI,
                        $"[Accounts] SelectedProfile set: '{value?.Id ?? ""}'");
                }
                catch (Exception ex)
                {
                    MLogger.Instance.Error(LogChannel.UI,
                        $"[Accounts] Failed to set SelectedProfile: {ex.Message}");
                }
            }
        }
    }

    // ==========================================================
    // Commands
    // ==========================================================
    public ICommand RefreshCommand { get; }
    public ICommand AddAccountCommand { get; }
    public ICommand TestCommand { get; }

    // ==========================================================
    // Ctor
    // ==========================================================
    public AccountsPageViewModel(
        IEmulator emulator,
        IPlatformRequirementGuard platformGuard,
        IAdbClient adbClient,
        IProfilesStore profilesStore,
        IAccountsSelector accountsSelector,
        ProfileDeviceBinder profileBinder,
        ProfileDeviceResolver profileResolver,
        IProfilesAutoBinder autoBinder,
        IAccountWindowsService accountWindowsService,
        IDialogService dialogs,
        IDeviceAutomation deviceAutomation)
    {
        _emulator = emulator ?? throw new ArgumentNullException(nameof(emulator));
        _platformGuard = platformGuard ?? throw new ArgumentNullException(nameof(platformGuard));
        _adb = adbClient ?? throw new ArgumentNullException(nameof(adbClient));

        _profilesStore = profilesStore ?? throw new ArgumentNullException(nameof(profilesStore));
        _accountsSelector = accountsSelector ?? throw new ArgumentNullException(nameof(accountsSelector));

        _profileBinder = profileBinder ?? throw new ArgumentNullException(nameof(profileBinder));
        _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));

        _autoBinder = autoBinder ?? throw new ArgumentNullException(nameof(autoBinder)); // ✅ NEW

        _accountWindows = accountWindowsService ?? throw new ArgumentNullException(nameof(accountWindowsService));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

        _deviceAutomation = deviceAutomation ?? throw new ArgumentNullException(nameof(deviceAutomation));

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        AddAccountCommand = new AsyncRelayCommand(AddAccountAsync);
        TestCommand = new AsyncRelayCommand(TestAsync);
    }

    // ==========================================================
    // IActivatable
    // ==========================================================
    public void OnEnter()
    {
        MLogger.Instance.Info(LogChannel.UI, $"[Accounts] Emulator impl = {_emulator.GetType().FullName}");

        if (_entered) return;
        _entered = true;

        // ✅ Subscribe once on enter (Core -> UI refresh)
        _profilesStore.ProfileChanged -= OnProfileChanged;
        _profilesStore.ProfileChanged += OnProfileChanged;

        var req = _platformGuard.Validate();
        if (!req.IsOk)
        {
            ShowNoFreeInstancesWarning = true;

            var steps = (req.FixSteps is null || req.FixSteps.Count == 0)
                ? ""
                : "\n\n- " + string.Join("\n- ", req.FixSteps);

            WarningText = $"{req.Title}\n{req.Message}{steps}";

            MLogger.Instance.Warn(LogChannel.UI,
                $"[Accounts] Emulator blocked: {req.Title} | {req.Message}");

            return;
        }

        // ✅ FIX: Warmup must run AFTER cards are built (after Dispatcher in Refresh)
        _ = RefreshFastAsync();

        MLogger.Instance.Info(LogChannel.UI,
            "[Accounts] OnEnter (Fast Refresh; Warmup scheduled after cards build)");
    }

    public void OnExit()
    {
        if (!_entered) return;
        _entered = false;

        // ✅ Unsubscribe on exit
        _profilesStore.ProfileChanged -= OnProfileChanged;

        foreach (var vm in Profiles)
            vm.DeleteRequested -= OnDeleteRequested;
    }

    // ==========================================================
    // ✅ Core -> UI refresh handler
    // ==========================================================
    private void OnProfileChanged(string profileId)
    {
        if (!_entered)
            return;

        // Prevent refresh storms caused by our own Save calls during refresh/warmup.
        if (_ignoreProfileChanged)
            return;

        // Coalesce multiple events into a single refresh.
        if (Interlocked.Exchange(ref _profileChangedScheduled, 1) == 1)
            return;

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                if (_entered)
                    await RefreshFastAsync().ConfigureAwait(false);
            }
            catch
            {
                // best-effort
            }
            finally
            {
                Interlocked.Exchange(ref _profileChangedScheduled, 0);
            }
        });
    }

    // ==========================================================
    // Gate helper
    // ==========================================================
    private async Task RunExclusiveAsync(Func<Task> action)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { await action().ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    // ==========================================================
    // Refresh (Fast UI-first)
    // ==========================================================
    private Task RefreshFastAsync()
        => RunExclusiveAsync(RefreshFastCoreAsync);

    private async Task RefreshFastCoreAsync()
    {
        _ignoreProfileChanged = true;
        try
        {
            // ✅ Ensure missing serials are auto-bound before building cards (best-effort)
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                await _autoBinder.AutoBindUnboundProfilesAsync(cts.Token).ConfigureAwait(false);
            }
            catch { /* best-effort */ }

            var instances = GetInstancesSafe()
                .OrderBy(i => GetStableInstanceSortKey(i))
                .ToList();

            var allProfiles = await _profilesStore.LoadAllAsync().ConfigureAwait(false);

            var existingIds = instances
                .Select(i => NormalizeId(i.InstanceId))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var shown = allProfiles
                .Where(p =>
                {
                    var pid = NormalizeId(p.InstanceId);
                    return !string.IsNullOrWhiteSpace(pid) && existingIds.Contains(pid);
                })
                .OrderBy(p => GetStableProfileSortKey(p))
                .ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Profiles.Clear();

                foreach (var p in shown)
                {
                    var pid = NormalizeId(p.InstanceId);

                    var inst = instances.FirstOrDefault(i =>
                        string.Equals(NormalizeId(i.InstanceId), pid, StringComparison.OrdinalIgnoreCase));

                    var baseLabel = inst is null
                        ? $"Instance #{p.InstanceId ?? "?"}"
                        : inst.Name;

                    var vm = new ProfileItemViewModel(
                        _profilesStore,
                        _accountWindows,
                        p,
                        baseLabel);

                    vm.DeleteRequested += OnDeleteRequested;
                    Profiles.Add(vm);
                }

                RestoreSelection();
                UpdateWarningFromInstances(instances.Count);
            });

            // ✅ FIX: Now cards exist => run warmup AFTER building cards
            _ = WarmupBindingAsync();
        }
        catch (Exception ex)
        {
            MLogger.Instance.Error(LogChannel.UI,
                $"[Accounts] FastRefresh failed: {ex}");
        }
        finally
        {
            _ignoreProfileChanged = false;
        }
    }

    private void UpdateWarningFromInstances(int instancesCount)
    {
        if (instancesCount <= 0)
        {
            ShowNoFreeInstancesWarning = true;
            WarningText = "No emulator instances found.";
        }
        else
        {
            ShowNoFreeInstancesWarning = false;
        }
    }

    // ==========================================================
    // Background Warmup (Progressive Resolve)
    // ==========================================================
    private async Task WarmupBindingAsync()
    {
        try
        {
            EnsureAdbServerLight();
            await Task.Delay(250).ConfigureAwait(false);

            foreach (var vm in Profiles)
            {
                var profile = vm.Model;

                var beforeGuid = profile.Binding?.BoundGuid;

                _profileResolver.Resolve(profile);

                var afterGuid = profile.Binding?.BoundGuid;

                if (!string.Equals(beforeGuid, afterGuid, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(afterGuid))
                {
                    await _profilesStore.SaveAsync(profile, CancellationToken.None)
                        .ConfigureAwait(false);
                }

                // ✅ refresh snapshot per profile (devices may appear late)
                var readyDevices = GetReadyDevicesSafe();

                var serial = profile.AdbSerial;
                bool online = false;

                if (!string.IsNullOrWhiteSpace(serial))
                {
                    serial = serial.Trim();
                    online = readyDevices.Any(d =>
                        string.Equals(d.Serial, serial, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.State, "device", StringComparison.OrdinalIgnoreCase));
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    vm.UpdateConnectionStatus(online, serial);
                });

                await Task.Delay(120).ConfigureAwait(false);
            }
        }
        catch
        {
            // best-effort
        }
    }

    // ==========================================================
    // Refresh Button
    // ==========================================================
    private Task RefreshAsync()
        => RefreshFastAsync();

    // ==========================================================
    // ✅ TEST BUTTON (Real device test using SelectedProfile.AdbSerial)
    // ==========================================================
    private Task TestAsync()
        => RunExclusiveAsync(TestCoreAsync);

    private async Task TestCoreAsync()
    {
        try
        {
            var vm = SelectedProfile;
            if (vm is null)
            {
                NotificationHub.Instance.Show(
                    "Test",
                    "Select an account first.",
                    NotificationType.Warning,
                    3500);
                return;
            }

            var serial = vm.Model.AdbSerial;
            if (string.IsNullOrWhiteSpace(serial))
            {
                NotificationHub.Instance.Show(
                    "Test",
                    "Selected account has no ADB serial. Refresh/bind a device first.",
                    NotificationType.Warning,
                    4500);
                return;
            }

            serial = serial.Trim();

            NotificationHub.Instance.Show(
                "Test",
                $"Running self-test on: {serial}",
                NotificationType.Info,
                3000);

            MLogger.Instance.Info(LogChannel.UI, $"[Accounts][Test] Start | Profile={vm.Id} | Serial={serial}");

            EnsureAdbServerLight();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            await _deviceAutomation.WaitForDeviceReadyAsync(serial, cts.Token).ConfigureAwait(false);

            var png = await _deviceAutomation.ScreenshotAsync(serial, cts.Token).ConfigureAwait(false);

            var outDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "SparkFlow_SelfTest");

            Directory.CreateDirectory(outDir);

            var file = Path.Combine(outDir, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            if (png is { Length: > 0 })
            {
                await File.WriteAllBytesAsync(file, png, cts.Token).ConfigureAwait(false);

                NotificationHub.Instance.Show(
                    "Test",
                    $"Screenshot saved: {file}",
                    NotificationType.Success,
                    5000);

                MLogger.Instance.Info(LogChannel.UI, $"[Accounts][Test] Screenshot saved: {file}");
            }
            else
            {
                NotificationHub.Instance.Show(
                    "Test",
                    "Screenshot failed (empty result).",
                    NotificationType.Warning,
                    4500);

                MLogger.Instance.Warn(LogChannel.UI, "[Accounts][Test] Screenshot returned empty bytes.");
            }

            await _deviceAutomation.TapAsync(serial, 540, 960, cts.Token).ConfigureAwait(false);
            MLogger.Instance.Info(LogChannel.UI, "[Accounts][Test] Tap executed at (540,960)");

            const string pkg = "com.camelgames.wo";
            var installed = await _deviceAutomation.IsPackageInstalledAsync(serial, pkg, cts.Token).ConfigureAwait(false);

            if (installed)
            {
                await _deviceAutomation.LaunchActivityAsync(
                    serial,
                    "com.camelgames.wo/com.camelgames.wo.MainActivity",
                    cts.Token).ConfigureAwait(false);

                NotificationHub.Instance.Show(
                    "Test",
                    "War and Order launch command sent ✅",
                    NotificationType.Success,
                    4000);

                MLogger.Instance.Info(LogChannel.UI, "[Accounts][Test] WarAndOrder launch command sent.");
            }
            else
            {
                NotificationHub.Instance.Show(
                    "Test",
                    "War and Order is not installed on this device.",
                    NotificationType.Warning,
                    4500);

                MLogger.Instance.Warn(LogChannel.UI, "[Accounts][Test] WarAndOrder not installed.");
            }

            NotificationHub.Instance.Show(
                "Test",
                "Self-test completed ✅",
                NotificationType.Success,
                3000);
        }
        catch (OperationCanceledException)
        {
            NotificationHub.Instance.Show(
                "Test",
                "Self-test timed out.",
                NotificationType.Warning,
                4500);
        }
        catch (Exception ex)
        {
            MLogger.Instance.Error(LogChannel.UI, $"[Accounts][Test] Failed: {ex}");
            NotificationHub.Instance.Show(
                "Test",
                $"Self-test failed: {ex.Message}",
                NotificationType.Error,
                6000);
        }
    }

    // ==========================================================
    // Add Account
    // ==========================================================
    private Task AddAccountAsync()
        => RunExclusiveAsync(AddAccountCoreAsync);

    private async Task AddAccountCoreAsync()
    {
        MLogger.Instance.Info(LogChannel.UI, "[Accounts] AddAccount CLICKED");

        try
        {
            var req = _platformGuard.Validate();
            if (!req.IsOk)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowNoFreeInstancesWarning = true;

                    var steps = (req.FixSteps is null || req.FixSteps.Count == 0)
                        ? ""
                        : "\n\n- " + string.Join("\n- ", req.FixSteps);

                    WarningText = $"{req.Title}\n{req.Message}{steps}";
                });

                return;
            }

            var instances = GetInstancesSafe()
                .OrderBy(i => GetStableInstanceSortKey(i))
                .ToList();

            if (instances.Count == 0)
            {
                NotificationHub.Instance.Show(
                    "Accounts",
                    "No emulator instances found.",
                    NotificationType.Warning,
                    4000);
                return;
            }

            var allProfiles = await _profilesStore.LoadAllAsync().ConfigureAwait(false);

            var usedInstanceIds = allProfiles
                .Select(p => NormalizeId(p.InstanceId))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var freeInstance = instances.FirstOrDefault(i =>
                !usedInstanceIds.Contains(NormalizeId(i.InstanceId) ?? string.Empty));

            if (freeInstance is null)
            {
                NotificationHub.Instance.Show(
                    "Accounts",
                    "No free instances available.",
                    NotificationType.Warning,
                    4000);
                return;
            }

            EnsureAdbServerLight();

            var candidateSerial = TryBuildSerialFromPort(freeInstance);
            if (!string.IsNullOrWhiteSpace(candidateSerial))
            {
                TryAdbConnect(candidateSerial);
                await Task.Delay(250).ConfigureAwait(false);
            }

            var devices = GetReadyDevicesSafe();

            var usedSerials = allProfiles
                .Where(p => !string.IsNullOrWhiteSpace(p.AdbSerial))
                .Select(p => p.AdbSerial!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var chosenSerial = devices
                .FirstOrDefault(d => !usedSerials.Contains(d.Serial))
                ?.Serial;

            var profile = new AccountProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = $"Account {allProfiles.Count + 1}",
                InstanceId = freeInstance.InstanceId,
                Active = false,
                AdbSerial = chosenSerial,
                CreatedAt = DateTimeOffset.Now,
                LastRun = null
            };

            if (!string.IsNullOrWhiteSpace(chosenSerial))
                _profileBinder.BindBestEffort(profile, chosenSerial);

            await _profilesStore.SaveAsync(profile, CancellationToken.None)
                .ConfigureAwait(false);

            // ✅ If serial was null, ask Core AutoBinder to fix it immediately
            if (string.IsNullOrWhiteSpace(profile.AdbSerial))
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    await _autoBinder.AutoBindUnboundProfilesAsync(cts.Token).ConfigureAwait(false);

                    // Reload same profile to reflect assigned serial (best-effort)
                    var all2 = await _profilesStore.LoadAllAsync().ConfigureAwait(false);
                    var updated = all2.FirstOrDefault(x => x.Id == profile.Id);
                    if (updated is not null)
                        profile = updated;
                }
                catch { /* best-effort */ }
            }

            NotificationHub.Instance.Show(
                "Accounts",
                $"Account added: {profile.Name} (Instance '{profile.InstanceId ?? "?"}')",
                NotificationType.Success,
                3500);

            await RefreshFastCoreAsync().ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SelectedProfile = Profiles.FirstOrDefault(x => x.Id == profile.Id);
            });
        }
        catch (Exception ex)
        {
            MLogger.Instance.Error(LogChannel.UI,
                $"[Accounts] AddAccount failed: {ex}");
        }
    }

    // ==========================================================
    // Delete
    // ==========================================================
    private void OnDeleteRequested(ProfileItemViewModel vm)
        => _ = DeleteProfileAsync(vm);

    private Task DeleteProfileAsync(ProfileItemViewModel vm)
        => RunExclusiveAsync(() => DeleteProfileCoreAsync(vm));

    private async Task DeleteProfileCoreAsync(ProfileItemViewModel vm)
    {
        var res = await _dialogs.ShowConfirmAsync(
            "Delete Account",
            $"Delete this account?\n\n- {vm.Name}",
            yesText: "Delete",
            noText: "Cancel").ConfigureAwait(false);

        if (!res.IsAccepted)
            return;

        await _profilesStore.DeleteAsync(vm.Id, CancellationToken.None)
            .ConfigureAwait(false);

        await RefreshFastCoreAsync().ConfigureAwait(false);
    }

    // ==========================================================
    // Helpers
    // ==========================================================
    private void RestoreSelection()
    {
        _suppressSelectionSave = true;
        try
        {
            var selectedId = _accountsSelector.SelectedProfileId;
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                var match = Profiles.FirstOrDefault(p => p.Id == selectedId);
                if (match is not null)
                {
                    SelectedProfile = match;
                    return;
                }
            }

            if (Profiles.Count > 0)
                SelectedProfile = Profiles[0];
        }
        finally
        {
            _suppressSelectionSave = false;
        }
    }

    private IReadOnlyList<IEmulatorInstance> GetInstancesSafe()
    {
        try { return _emulator.ScanInstances(); }
        catch { return Array.Empty<IEmulatorInstance>(); }
    }

    private IReadOnlyList<AdbDevice> GetReadyDevicesSafe()
    {
        try
        {
            return _adb.Devices()
                .Where(d => d.State == "device")
                .ToList();
        }
        catch
        {
            return Array.Empty<AdbDevice>();
        }
    }

    private void EnsureAdbServerLight()
    {
        try { _adb.StartServer(); }
        catch { }
    }

    private static string? NormalizeId(string? id)
        => string.IsNullOrWhiteSpace(id) ? null : id.Trim();

    private static int ExtractTrailingNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return int.MaxValue;

        var s = text.Trim();
        var i = s.Length - 1;

        while (i >= 0 && char.IsDigit(s[i])) i--;

        var digits = s[(i + 1)..];
        return int.TryParse(digits, out var n) ? n : int.MaxValue;
    }

    private static (int num, string id) GetStableInstanceSortKey(IEmulatorInstance inst)
    {
        var id = NormalizeId(inst.InstanceId) ?? "";
        return (ExtractTrailingNumber(id), id);
    }

    private static (int num, string id) GetStableProfileSortKey(AccountProfile profile)
    {
        var id = NormalizeId(profile.InstanceId) ?? "";
        return (ExtractTrailingNumber(id), id);
    }

    private static string? TryBuildSerialFromPort(IAdbPortProvider inst)
    {
        try
        {
            if (inst.AdbPort is int port && port >= 5555)
                return $"127.0.0.1:{port}";
        }
        catch { }

        return null;
    }

    private void TryAdbConnect(string serial)
    {
        try { _adb.RunRaw($"connect {serial}", timeoutMs: 4000); }
        catch { }
    }
}

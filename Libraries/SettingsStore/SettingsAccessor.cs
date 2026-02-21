using SettingsStore.Interfaces;
using SettingsStore.Models;

namespace SettingsStore;

/// <summary>
/// Default implementation of ISettingsAccessor.
/// Used by Core and UI layers.
/// </summary>
public sealed class SettingsAccessor : ISettingsAccessorAsync
{
    private readonly ISettingsProvider _provider;
    private readonly ISettingsProviderAsync? _providerAsync;

    private readonly object _gate = new();

    /// <summary>
    /// Legacy singleton access (kept for compatibility).
    /// Prefer DI instead.
    /// </summary>
    public static SettingsAccessor? Instance { get; private set; }

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsAccessor(ISettingsProvider provider)
    {
        _provider = provider;
        _providerAsync = provider as ISettingsProviderAsync;

        Current = _provider.Load();

        // Avoid accidental overwrite if multiple instances are created.
        Instance ??= this;
    }

    public AppSettings Current { get; private set; }

    /// <summary>
    /// Thread-safe settings mutation helper.
    /// Ensures consistent changes + notifies listeners.
    /// </summary>
    public void Update(Action<AppSettings> mutate)
    {
        if (mutate == null) throw new ArgumentNullException(nameof(mutate));

        AppSettings snapshot;
        lock (_gate)
        {
            mutate(Current);
            snapshot = Current;
        }

        SettingsChanged?.Invoke(this, snapshot);
    }

    public void Save()
    {
        AppSettings snapshot;
        lock (_gate)
        {
            // (Optional) Validate(Current) here before saving
            snapshot = Current;
        }

        _provider.Save(snapshot);
        SettingsChanged?.Invoke(this, snapshot);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        AppSettings snapshot;
        lock (_gate)
        {
            // (Optional) Validate(Current) here before saving
            snapshot = Current;
        }

        if (_providerAsync != null)
            await _providerAsync.SaveAsync(snapshot, ct).ConfigureAwait(false);
        else
            _provider.Save(snapshot);

        SettingsChanged?.Invoke(this, snapshot);
    }

    public void Reset()
    {
        AppSettings snapshot;
        lock (_gate)
        {
            Current = _provider.Reset();
            snapshot = Current;
        }

        SettingsChanged?.Invoke(this, snapshot);
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        AppSettings snapshot;
        if (_providerAsync != null)
        {
            var loaded = await _providerAsync.ResetAsync(ct).ConfigureAwait(false);
            lock (_gate) { Current = loaded; snapshot = Current; }
        }
        else
        {
            lock (_gate) { Current = _provider.Reset(); snapshot = Current; }
        }

        SettingsChanged?.Invoke(this, snapshot);
    }
}
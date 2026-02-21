/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Core/Engine/MultiAccount/InstanceSwitcher.cs
 * ============================================================================ */


using SparkFlow.Abstractions.Engine.Interfaces;
using SparkFlow.Abstractions.Services.Emulator.Guards;
using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Engine.Engine.MultiAccount;

public sealed class InstanceSwitcher : IInstanceSwitcher
{
    private readonly IEmulatorInstanceControlService _emu;

    public InstanceSwitcher(IEmulatorInstanceControlService emu)
    {
        _emu = emu;
    }

    public async Task SwitchToAsync(IAccountState account, CancellationToken ct)
    {
        if (!account.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(account.InstanceId))
            return;

        if (account.InstanceId == "0")
            return;

        await _emu.StartAsync(account.InstanceId, true, 90_000, ct);
    }
}
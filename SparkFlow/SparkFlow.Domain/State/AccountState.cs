namespace SparkFlow.Domain.State;

public sealed class AccountState
{
    // Identity
    public string InstanceId { get; }
    public string Name { get; private set; }

    // Scheduling
    public int Order { get; private set; }
    public bool Enabled { get; private set; }

    // Mapping
    public string? AdbSerial { get; private set; }

    // Card badges
    public string RunnerStateText { get; private set; } = "Runner: Idle";
    public string AdbStatusText { get; private set; } = "ADB: —";
    public string InstanceStatusText { get; private set; } = "Instance: —";
    public string TutorialStatusText { get; private set; } = "Tutorial: —";

    // Card extra
    public string LastActiveText { get; private set; } = "Last active: —";
    public string LastSnapshotText { get; private set; } = "Snapshot: —";

    public event Action<AccountState>? Changed;

    public AccountState(string instanceId, string name, bool enabled, int order)
    {
        instanceId = string.IsNullOrWhiteSpace(instanceId) ? "" : instanceId.Trim();
        InstanceId = instanceId;

        Name = string.IsNullOrWhiteSpace(name) ? $"Instance {instanceId}" : name.Trim();
        Enabled = enabled;
        Order = order;
    }

    public void SetEnabled(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        Changed?.Invoke(this);
    }

    public void SetOrder(int order)
    {
        if (Order == order) return;
        Order = order;
        Changed?.Invoke(this);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();
        if (Name == name) return;
        Name = name;
        Changed?.Invoke(this);
    }

    public void SetAdbSerial(string? serial)
    {
        serial = string.IsNullOrWhiteSpace(serial) ? null : serial.Trim();
        if (AdbSerial == serial) return;
        AdbSerial = serial;
        Changed?.Invoke(this);
    }

    public void SetBadges(
        string? runnerStateText = null,
        string? adbStatusText = null,
        string? instanceStatusText = null,
        string? tutorialStatusText = null)
    {
        var changed = false;

        if (!string.IsNullOrWhiteSpace(runnerStateText) && RunnerStateText != runnerStateText)
        {
            RunnerStateText = runnerStateText;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(adbStatusText) && AdbStatusText != adbStatusText)
        {
            AdbStatusText = adbStatusText;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(instanceStatusText) && InstanceStatusText != instanceStatusText)
        {
            InstanceStatusText = instanceStatusText;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(tutorialStatusText) && TutorialStatusText != tutorialStatusText)
        {
            TutorialStatusText = tutorialStatusText;
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this);
    }

    public void SetLastTexts(string? lastActiveText = null, string? lastSnapshotText = null)
    {
        var changed = false;

        if (!string.IsNullOrWhiteSpace(lastActiveText) && LastActiveText != lastActiveText)
        {
            LastActiveText = lastActiveText;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(lastSnapshotText) && LastSnapshotText != lastSnapshotText)
        {
            LastSnapshotText = lastSnapshotText;
            changed = true;
        }

        if (changed)
            Changed?.Invoke(this);
    }
}
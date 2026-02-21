/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/State/Interfaces/IAccountState.cs
 * Purpose: Core component: IAccountState.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace SparkFlow.Abstractions.State.Interfaces;

/// <summary>
/// Source-of-truth state for one account (one emulator instance profile).
/// </summary>
public interface IAccountState
{
    // Identity
    string InstanceId { get; }
    string Name { get; }

    // Scheduling
    int Order { get; }
    bool Enabled { get; }

    // Mapping / execution
    string? AdbSerial { get; }

    // Card badges
    string RunnerStateText { get; }
    string AdbStatusText { get; }
    string InstanceStatusText { get; }
    string TutorialStatusText { get; }

    // Card extra info
    string LastActiveText { get; }
    string LastSnapshotText { get; }

    // Change event (UI / runner can subscribe)
    event Action<IAccountState>? Changed;

    // Mutations (controlled setters)
    void SetEnabled(bool enabled);
    void SetOrder(int order);
    void SetName(string name);

    void SetAdbSerial(string? serial);

    void SetBadges(
        string? runnerStateText = null,
        string? adbStatusText = null,
        string? instanceStatusText = null,
        string? tutorialStatusText = null);

    void SetLastTexts(string? lastActiveText = null, string? lastSnapshotText = null);
}

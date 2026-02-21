/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlowBot/SparkFlowBot.Core/Engine/MultiAccount/AccountQueue.cs
 * Purpose: Core component: AccountQueue.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 * ============================================================================ */

using SparkFlow.Abstractions.State.Interfaces;

namespace SparkFlow.Engine.Engine.MultiAccount;

public sealed class AccountQueue : IAccountQueue
{
    private readonly object _gate = new();
    private List<IAccountState> _enabledOrdered = new();
    private int _cursor;

    public void Rebuild(IEnumerable<IAccountState> allAccounts)
    {
        if (allAccounts is null) throw new ArgumentNullException(nameof(allAccounts));

        lock (_gate)
        {
            _enabledOrdered = allAccounts
                .Where(a => a is not null && a.Enabled)
                .Where(a => !string.IsNullOrWhiteSpace(a.InstanceId))
                .Where(a =>
                {
                    var id = a.InstanceId.Trim();
                    return id != "-1" && id != "0";
                })
                .OrderBy(a => a.Order)
                .ThenBy(a => a.InstanceId)
                .ToList();

            _cursor = 0;
        }
    }

    public IAccountState? Next()
    {
        lock (_gate)
        {
            if (_enabledOrdered.Count == 0) return null;
            if (_cursor >= _enabledOrdered.Count) return null;

            var item = _enabledOrdered[_cursor];
            _cursor++;
            return item;
        }
    }

    public IAccountState? Peek()
    {
        lock (_gate)
        {
            if (_enabledOrdered.Count == 0) return null;
            if (_cursor >= _enabledOrdered.Count) return null;

            return _enabledOrdered[_cursor];
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _cursor = 0;
        }
    }

    public int Count
    {
        get { lock (_gate) return _enabledOrdered.Count; }
    }

    public int Remaining
    {
        get
        {
            lock (_gate)
            {
                var rem = _enabledOrdered.Count - _cursor;
                return rem < 0 ? 0 : rem;
            }
        }
    }

    public IReadOnlyList<IAccountState> Snapshot()
    {
        lock (_gate)
        {
            return _enabledOrdered.ToList();
        }
    }
}

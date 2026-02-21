/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Abstractions/IAdbMapping.cs
 * Purpose: Library component: IAdbMapping.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdbLib.Abstractions;

public interface IAdbMapping
{
    Task<IReadOnlyDictionary<int, string>> BuildMappingAsync(
        IReadOnlyList<int> instanceIds,
        Func<int, Task> launchInstanceAsync,
        Func<int, Task> stopInstanceAsync,
        CancellationToken ct);

    Task<string> ResolveSerialForInstanceAsync(
        int instanceId,
        Func<int, Task> launchInstanceAsync,
        Func<int, Task> stopInstanceAsync,
        CancellationToken ct);
}
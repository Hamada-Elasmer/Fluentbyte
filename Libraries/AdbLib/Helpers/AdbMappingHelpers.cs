/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Helpers/AdbMappingHelpers.cs
 * Purpose: Library component: AdbMappingHelpers.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;
using System.Collections.Generic;

namespace AdbLib.Helpers;

public static class AdbMappingHelpers
{
    /// <summary>
    /// Generic helper to resolve ADB serial from a list of "instance info" models without referencing any emulator library.
    /// Example usage with your emulator list2:
    /// AdbMappingHelpers.TryResolveSerialFromList(list2, x => x.Index, x => x.AdbPort, instanceId);
    /// </summary>
    public static string? TryResolveSerialFromList<T>(
        IReadOnlyList<T> items,
        Func<T, int> getInstanceId,
        Func<T, int?> getAdbPort,
        int instanceId,
        string host = "127.0.0.1")
    {
        if (items is null) return null;
        if (getInstanceId is null) throw new ArgumentNullException(nameof(getInstanceId));
        if (getAdbPort is null) throw new ArgumentNullException(nameof(getAdbPort));

        foreach (var item in items)
        {
            if (getInstanceId(item) != instanceId) continue;

            var port = getAdbPort(item);
            if (port is null) return null;

            return $"{host}:{port.Value}";
        }

        return null;
    }
}
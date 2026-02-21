/* ============================================================================
 * SparkFlow File Header
 * File: SparkFlow.Abstractions/Services/Logging/ILogHub.cs
 * Purpose: Core component: ILogHub.
 * Notes:
 *  - Central in-memory log store for UI binding.
 *  - Entries are appended by LogHub via MLogger.LogEvent subscription.
 * ============================================================================ */

using System.Collections.ObjectModel;
using UtiliLib.Models;

namespace SparkFlow.Abstractions.Services.Logging;

public interface ILogHub
{
    /// <summary>
    /// Central in-memory log store (UI can bind to it).
    /// </summary>
    ObservableCollection<LogEntry> Entries { get; }

    /// <summary>
    /// Clears all stored logs.
    /// </summary>
    void Clear();
}
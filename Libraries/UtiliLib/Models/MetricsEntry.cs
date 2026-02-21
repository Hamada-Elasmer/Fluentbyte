/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Models/MetricsEntry.cs
 * Purpose: Library component: MetricsEntry.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using UtiliLib.Types;

namespace UtiliLib.Models;

public class MetricsEntry
{
    public string Key { get; set; }
    public int Passes { get; set; }
    public int Fails { get; set; }
    public MetricsTypes Type { get; set; }

    public MetricsEntry()
    {
        Key = string.Empty;
        Passes = 0;
        Fails = 0;
        Type = MetricsTypes.IMAGE; // Make sure you have a default value.
    }
}
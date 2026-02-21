/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Models/MetricsPass.cs
 * Purpose: Library component: MetricsPass.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using UtiliLib.Types;

namespace UtiliLib.Models;

public class MetricsPass
{
    public string Key { get; set; }
    public bool Passed { get; set; }
    public MetricsTypes Type { get; set; }

    public MetricsPass()
    {
        Key = string.Empty;
        Passed = false;
        Type = MetricsTypes.IMAGE; // Make sure you have a default value. MetricsTypes.Unknown
    }
}
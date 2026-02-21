/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Options/PortScannerOptions.cs
 * Purpose: Library component: PortScannerOptions.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace UtiliLib.Options
{
    public class PortScannerOptions
    {
        public bool AllowAggressiveReclaim { get; set; } = false;
        public int MaxRetries { get; set; } = 2;
        public int RetryDelayMs { get; set; } = 500;

        public int FreePortStart { get; set; } = 5000;
        public int FreePortEnd { get; set; } = 65000;
    }
}
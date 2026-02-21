/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Infrastructure/Network/PortDetail.cs
 * Purpose: Library component: PortDetail.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace UtiliLib.Helpers
{
    /// <summary>
    /// Represents the details of a network port and the associated process.
    /// </summary>
    public class PortDetail
    {
        public int Port { get; set; }
        public int ProcessId { get; set; }

        // Must be public so the scanner can populate it.
        public string ProcessName { get; set; } = string.Empty;

        // Renamed from Path to avoid conflict with System.IO.Path.
        public string ExecutablePath { get; set; } = string.Empty;

        public override string ToString()
            => $"Port: {Port}, PID: {ProcessId}, Name: {ProcessName}, Path: {ExecutablePath}";
    }
}
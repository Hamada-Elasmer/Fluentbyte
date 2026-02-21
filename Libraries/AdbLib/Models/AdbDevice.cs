/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Models/AdbDevice.cs
 * Purpose: Library component: AdbDevice.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace AdbLib.Models;

public sealed record AdbDevice(
    string Serial,
    string State,
    string? Product = null,
    string? Model = null,
    string? Device = null
);
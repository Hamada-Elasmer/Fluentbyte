/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Services/SystemClock.cs
 * Purpose: Library component: SystemClock.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using LicenseLib.Abstractions;

namespace LicenseLib.Services;

public sealed class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}
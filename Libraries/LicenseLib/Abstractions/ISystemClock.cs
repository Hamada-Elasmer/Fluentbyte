/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/LicenseLib/Abstractions/ISystemClock.cs
 * Purpose: Library component: ISystemClock.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

namespace LicenseLib.Abstractions;

public interface ISystemClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}
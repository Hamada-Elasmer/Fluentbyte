/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Exceptions/AdbDeviceNotFoundException.cs
 * Purpose: Library component: AdbDeviceNotFoundException.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;

namespace AdbLib.Exceptions;

public sealed class AdbDeviceNotFoundException : Exception
{
    public AdbDeviceNotFoundException(string message) : base(message) { }
}
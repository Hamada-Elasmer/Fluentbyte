/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Exceptions/AdbProvisioningException.cs
 * Purpose: Library component: AdbProvisioningException.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;

namespace AdbLib.Exceptions;

public sealed class AdbProvisioningException : Exception
{
    public AdbProvisioningException(string message) : base(message) { }
    public AdbProvisioningException(string message, Exception inner) : base(message, inner) { }
}
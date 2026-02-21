/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Exceptions/AdbMappingException.cs
 * Purpose: Library component: AdbMappingException.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System;

namespace AdbLib.Exceptions;

public sealed class AdbMappingException : Exception
{
    public AdbMappingException(string message) : base(message) { }
}
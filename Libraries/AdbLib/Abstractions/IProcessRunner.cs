/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/AdbLib/Abstractions/IProcessRunner.cs
 * Purpose: Library component: IProcessRunner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Threading;
using System.Threading.Tasks;

namespace AdbLib.Abstractions;

public interface IProcessRunner
{
    Task<(int exitCode, string stdout, string stderr)> RunAsync(
        string fileName,
        string arguments,
        int timeoutMs = 30_000,
        CancellationToken ct = default);
}
/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Abstractions/IPortScanner.cs
 * Purpose: Library component: IPortScanner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Generic;
using UtiliLib.Helpers;

namespace UtiliLib.Abstractions
{
    public interface IPortScanner
    {
        bool IsPortInUse(int port);

        (bool ok, PortDetail detail) TryGetPortDetail(int port);

        int FindFreePort(int startPort = 5000, int endPort = 65000);

        IReadOnlyDictionary<int, PortDetail> GetAllListeningPorts();
    }
}
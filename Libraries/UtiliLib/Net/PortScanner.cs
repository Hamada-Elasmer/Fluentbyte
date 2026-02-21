/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Net/PortScanner.cs
 * Purpose: Library component: PortScanner.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Collections.Generic;
using UtiliLib.Abstractions;
using UtiliLib.Helpers;
using UtiliLib.Infrastructure.Network;
using UtiliLib.Options;

namespace UtiliLib.Net
{
    public class PortScanner : IPortScanner
    {
        private readonly TcpService _tcp = new TcpService();
        private readonly PortScannerOptions _opts;

        public PortScanner(PortScannerOptions opts)
        {
            _opts = opts;
        }

        public bool IsPortInUse(int port) => _tcp.IsPortInUse(port);

        public (bool ok, PortDetail detail) TryGetPortDetail(int port)
        {
            var t = _tcp.GetPortDetails(port);
            return (t.Item1, t.Item2);
        }

        public int FindFreePort(int startPort = 5000, int endPort = 65000)
        {
            var s = startPort > 0 ? startPort : _opts.FreePortStart;
            var e = endPort > 0 ? endPort : _opts.FreePortEnd;
            return _tcp.FindFreePort(s, e);
        }

        public IReadOnlyDictionary<int, PortDetail> GetAllListeningPorts()
            => _tcp.GetAllListeningPorts();
    }
}
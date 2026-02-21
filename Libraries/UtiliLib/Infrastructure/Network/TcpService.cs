/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Infrastructure/Network/TcpService.cs
 * Purpose: Library component: for.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

#nullable enable
using System.Diagnostics;
using System.Text.RegularExpressions;
using UtiliLib.Helpers;

namespace UtiliLib.Infrastructure.Network
{
    /// <summary>
    /// Utility class for TCP port-related operations.
    /// Windows only: uses netstat -ano.
    /// </summary>
    public class TcpService
    {
        // TCP    0.0.0.0:5555     0.0.0.0:0     LISTENING     1234
        private static readonly Regex NetstatRegex = new Regex(
            @"^\s*(TCP|UDP)\s+(?<local>\S+)\s+(?<remote>\S+)\s+(?<state>LISTENING)?\s*(?<pid>\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public Tuple<bool, PortDetail> GetPortDetails(int port)
        {
            try
            {
                var map = GetListeningPortPidMap();
                if (!map.TryGetValue(port, out var pid))
                    return Tuple.Create(false, new PortDetail { Port = port });

                var (name, path) = QueryProcessInfo(pid);

                var detail = new PortDetail
                {
                    Port = port,
                    ProcessId = pid,
                    ProcessName = name,
                    ExecutablePath = path
                };

                return Tuple.Create(true, detail);
            }
            catch
            {
                return Tuple.Create(false, new PortDetail { Port = port });
            }
        }

        public bool IsPortInUse(int port)
        {
            try
            {
                return GetListeningPortPidMap().ContainsKey(port);
            }
            catch
            {
                // If netstat fails, be conservative and assume the port is in-use.
                return true;
            }
        }

        public int FindFreePort(int startPort = 5000, int endPort = 65000)
        {
            if (startPort < 1 || endPort > 65535 || startPort >= endPort)
                throw new ArgumentOutOfRangeException(nameof(startPort));

            var used = GetListeningPortPidMap().Keys.ToHashSet();
            for (int p = startPort; p <= endPort; p++)
            {
                if (!used.Contains(p))
                    return p;
            }

            throw new InvalidOperationException("No free port found in the given range.");
        }

        public Dictionary<int, PortDetail> GetAllListeningPorts()
        {
            var map = GetListeningPortPidMap();
            var result = new Dictionary<int, PortDetail>();

            foreach (var kv in map)
            {
                var (name, path) = QueryProcessInfo(kv.Value);
                result[kv.Key] = new PortDetail
                {
                    Port = kv.Key,
                    ProcessId = kv.Value,
                    ProcessName = name,
                    ExecutablePath = path
                };
            }

            return result;
        }

        // -----------------------------
        // Internal
        // -----------------------------
        private Dictionary<int, int> GetListeningPortPidMap()
        {
            var output = ExecuteCommand("netstat", "-ano -p tcp");
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var map = new Dictionary<int, int>();

            foreach (var line in lines)
            {
                var m = NetstatRegex.Match(line);
                if (!m.Success) continue;

                var state = m.Groups["state"].Value;
                if (!string.Equals(state, "LISTENING", StringComparison.OrdinalIgnoreCase))
                    continue;

                var local = m.Groups["local"].Value;
                var pidStr = m.Groups["pid"].Value;

                if (!int.TryParse(pidStr, out var pid))
                    continue;

                var port = ExtractPort(local);
                if (port == null) continue;

                map[port.Value] = pid;
            }

            return map;
        }

        private int? ExtractPort(string localEndpoint)
        {
            // 0.0.0.0:5555
            // 127.0.0.1:5037
            // [::]:5555
            var idx = localEndpoint.LastIndexOf(':');
            if (idx <= 0 || idx >= localEndpoint.Length - 1)
                return null;

            var portStr = localEndpoint[(idx + 1)..].Trim();
            if (int.TryParse(portStr, out var port))
                return port;

            return null;
        }

        private Tuple<string, string> QueryProcessInfo(int pid)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                var name = p.ProcessName ?? string.Empty;

                string path = string.Empty;
                try
                {
                    path = p.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    // MainModule can fail due to permissions.
                }

                return Tuple.Create(name, path);
            }
            catch
            {
                return Tuple.Create(string.Empty, string.Empty);
            }
        }

        private string ExecuteCommand(string command, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var p = Process.Start(psi);
            if (p == null) return string.Empty;

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();

            p.WaitForExit(5000);

            return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
        }
    }
}
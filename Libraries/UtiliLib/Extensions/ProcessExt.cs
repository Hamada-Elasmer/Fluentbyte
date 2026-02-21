/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Extensions/ProcessExt.cs
 * Purpose: Library component: for.
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UtiliLib.Extensions
{
    /// <summary>
    /// Extension class for process operations like suspend and resume.
    /// </summary>
    public static class ProcessExt
    {
        // ================================
        // P/Invoke Windows API
        // ================================
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        // ================================
        // Public Methods
        // ================================

        /// <summary>
        /// Resume processes by their names.
        /// </summary>
        /// <param name="processNames">Array of process names to resume.</param>
        /// <returns>True if all processes resumed successfully.</returns>
        public static bool ResumeProcessByName(string[] processNames)
        {
            bool result = true;
            foreach (var name in processNames)
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                    {
                        ResumeProcess(proc.Id);
                    }
                }
                catch
                {
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Suspend processes by their names.
        /// </summary>
        /// <param name="processNames">Array of process names to suspend.</param>
        /// <returns>True if all processes suspended successfully.</returns>
        public static bool SuspendProcessByName(string[] processNames)
        {
            bool result = true;
            foreach (var name in processNames)
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                    {
                        SuspendProcess(proc.Id);
                    }
                }
                catch
                {
                    result = false;
                }
            }
            return result;
        }

        // ================================
        // Private Methods
        // ================================
        /// <summary>
        /// Suspend a process by its process ID.
        /// </summary>
        /// <param name="pid">Process ID to suspend.</param>
        private static void SuspendProcess(int pid)
        {
            var process = Process.GetProcessById(pid);
            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr threadHandle = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                if (threadHandle != IntPtr.Zero)
                {
                    SuspendThread(threadHandle);
                    CloseHandle(threadHandle);
                }
            }
        }

        /// <summary>
        /// Resume a process by its process ID.
        /// </summary>
        /// <param name="pid">Process ID to resume.</param>
        private static void ResumeProcess(int pid)
        {
            var process = Process.GetProcessById(pid);
            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr threadHandle = OpenThread(ThreadAccess.SuspendResume, false, (uint)thread.Id);
                if (threadHandle != IntPtr.Zero)
                {
                    ResumeThread(threadHandle);
                    CloseHandle(threadHandle);
                }
            }
        }

        // ================================
        // Private Enum for Thread access flags
        // ================================
        [Flags]
        private enum ThreadAccess
        {
            Terminate = 1,
            SuspendResume = 2,
            GetContext = 8,
            SetContext = 16,
            SetInformation = 32,
            QueryInformation = 64,
            SetThreadToken = 128,
            Impersonate = 256,
            DirectImpersonation = 512
        }
    }
}
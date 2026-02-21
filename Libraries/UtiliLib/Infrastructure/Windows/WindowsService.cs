/* ============================================================================
 * SparkFlow File Header
 * File: Libraries/UtiliLib/Infrastructure/Windows/WindowsService.cs
 * Purpose: Library component: Windows helper utilities (find/close/move/resize).
 * Notes:
 *  - This file is part of the SparkFlow automation platform.
 *  - Comments are intentionally kept in English for consistency across the codebase.
 * ============================================================================ */

using System.Runtime.InteropServices;
using System.Text;

namespace UtiliLib.Infrastructure.Windows
{
    public static class WindowsService
    {
        // ================================
        // Constants
        // ================================
        public const int WmClose = 0x0010; // WM_CLOSE message

        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        // ================================
        // Structs
        // ================================
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        // ================================
        // P/Invoke Methods
        // ================================
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextInternal(IntPtr hWnd, StringBuilder text, int maxLength);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);

        // DPI-aware (Win10+). If fails, fallback to AdjustWindowRectEx.
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AdjustWindowRectExForDpi(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle, uint dpi);

        // NEW: client/window rect + visibility
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        // ================================
        // Window Methods
        // ================================
        public static string GetWindowText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return string.Empty;

            var sb = new StringBuilder(length + 1);
            GetWindowTextInternal(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool CloseWindowByCaption(string caption, bool exactMatch = false)
        {
            foreach (var hWnd in exactMatch ? FindWindowsWithExactText(caption) : FindWindowsWithText(caption))
            {
                SendMessage(hWnd, WmClose, IntPtr.Zero, IntPtr.Zero);
            }
            return true;
        }

        public static IEnumerable<IntPtr> FindWindowsWithText(string text)
            => FindWindows((hWnd, _) => GetWindowText(hWnd).Contains(text));

        public static IEnumerable<IntPtr> FindWindowsWithExactText(string text)
            => FindWindows((hWnd, _) => GetWindowText(hWnd) == text);

        public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc callback)
        {
            var results = new List<IntPtr>();

            EnumWindows((hWnd, lParam) =>
            {
                if (callback(hWnd, lParam))
                    results.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            return results;
        }

        public static (int width, int height) GetClientSize(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return (0, 0);
            if (!GetClientRect(hWnd, out var rc)) return (0, 0);
            return (Math.Max(0, rc.Right - rc.Left), Math.Max(0, rc.Bottom - rc.Top));
        }

        public static (int width, int height) GetWindowSize(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return (0, 0);
            if (!GetWindowRect(hWnd, out var rc)) return (0, 0);
            return (Math.Max(0, rc.Right - rc.Left), Math.Max(0, rc.Bottom - rc.Top));
        }

        // ================================
        // Process / Window binding
        // ================================
        public static IntPtr FindMainWindowByProcessId(int processId)
        {
            var windows = FindWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out var pid);
                if (pid != (uint)processId) return false;

                // Caption may be empty; visibility is a better heuristic for dnplayer
                return IsWindowVisible(hWnd);
            });

            return windows.FirstOrDefault();
        }

        public static async Task<IntPtr> WaitForMainWindowAsync(int processId, int retries = 60, int delayMs = 250)
        {
            if (retries <= 0) retries = 30;
            if (delayMs <= 0) delayMs = 200;

            for (int i = 0; i < retries; i++)
            {
                var hWnd = FindMainWindowByProcessId(processId);
                if (hWnd != IntPtr.Zero) return hWnd;

                await Task.Delay(delayMs).ConfigureAwait(false);
            }

            return IntPtr.Zero;
        }

        public static bool ApplyWindowRect(
            IntPtr hWnd,
            int x, int y,
            int width, int height,
            bool maximized,
            bool minimized)
        {
            if (hWnd == IntPtr.Zero) return false;

            ShowWindow(hWnd, SW_RESTORE);

            var ok = SetWindowPos(
                hWnd, HWND_TOP,
                x, y, width, height,
                SWP_NOZORDER | SWP_NOACTIVATE);

            if (!ok) return false;

            if (maximized) ShowWindow(hWnd, SW_MAXIMIZE);
            else if (minimized) ShowWindow(hWnd, SW_MINIMIZE);

            return true;
        }

        public static bool ApplyWindowClientRect(
            IntPtr hWnd,
            int x, int y,
            int clientWidth, int clientHeight,
            bool maximized,
            bool minimized)
        {
            if (hWnd == IntPtr.Zero) return false;
            if (clientWidth < 50 || clientHeight < 50) return false;

            ShowWindow(hWnd, SW_RESTORE);

            var style = GetWindowLong(hWnd, GWL_STYLE);
            var exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

            var rc = new RECT { Left = 0, Top = 0, Right = clientWidth, Bottom = clientHeight };

            bool okAdjust;

            try
            {
                var dpi = GetDpiForWindow(hWnd);
                okAdjust = AdjustWindowRectExForDpi(ref rc, style, false, exStyle, dpi);
            }
            catch
            {
                okAdjust = false;
            }

            if (!okAdjust)
                okAdjust = AdjustWindowRectEx(ref rc, style, false, exStyle);

            if (!okAdjust)
                return false;

            var outerW = rc.Right - rc.Left;
            var outerH = rc.Bottom - rc.Top;

            var ok = SetWindowPos(
                hWnd, HWND_TOP,
                x, y, outerW, outerH,
                SWP_NOZORDER | SWP_NOACTIVATE);

            if (!ok) return false;

            if (maximized) ShowWindow(hWnd, SW_MAXIMIZE);
            else if (minimized) ShowWindow(hWnd, SW_MINIMIZE);

            return true;
        }

        /// <summary>
        /// Stable client sizing: keeps applying until the window CLIENT size matches target.
        /// Useful for emulators that override window size after splash/render.
        /// </summary>
        public static async Task<bool> ApplyWindowClientRectStable(
            IntPtr hWnd,
            int x, int y,
            int clientWidth, int clientHeight,
            bool maximized,
            bool minimized,
            int stableMs = 1200,
            int overallTimeoutMs = 12000,
            int tickMs = 250)
        {
            if (hWnd == IntPtr.Zero) return false;
            if (clientWidth < 50 || clientHeight < 50) return false;

            if (tickMs < 50) tickMs = 50;
            if (overallTimeoutMs < 1000) overallTimeoutMs = 1000;
            if (stableMs < 200) stableMs = 200;

            ShowWindow(hWnd, SW_RESTORE);

            var deadline = Environment.TickCount + overallTimeoutMs;
            var stableStart = -1;

            while (Environment.TickCount < deadline)
            {
                var okApply = ApplyWindowClientRect(hWnd, x, y, clientWidth, clientHeight, maximized, minimized);
                if (!okApply)
                    return false;

                await Task.Delay(tickMs).ConfigureAwait(false);

                var (cw, ch) = GetClientSize(hWnd);

                // allow small tolerance (DPI/theme can cause off-by-1/2)
                var match = Math.Abs(cw - clientWidth) <= 2 && Math.Abs(ch - clientHeight) <= 2;

                if (match)
                {
                    if (stableStart < 0)
                        stableStart = Environment.TickCount;

                    if (Environment.TickCount - stableStart >= stableMs)
                        return true;
                }
                else
                {
                    stableStart = -1;
                }
            }

            var (finalW, finalH) = GetClientSize(hWnd);
            return Math.Abs(finalW - clientWidth) <= 2 && Math.Abs(finalH - clientHeight) <= 2;
        }

        // ================================
        // Process Methods
        // ================================
        public static async Task KillOnlyProcess(int processId)
        {
            await Task.Run(() =>
            {
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(processId);
                    process.Kill();
                }
                catch
                {
                    // ignore
                }
            }).ConfigureAwait(false);
        }

        public static async Task KillProcess(int processId)
            => await KillOnlyProcess(processId).ConfigureAwait(false);

        public static async Task<List<int>> FindProcessesByName(string name)
        {
            return await Task.Run(() =>
            {
                var results = new List<int>();
                try
                {
                    foreach (var process in System.Diagnostics.Process.GetProcessesByName(name))
                    {
                        results.Add(process.Id);
                    }
                }
                catch
                {
                    // ignore
                }
                return results;
            }).ConfigureAwait(false);
        }

        // ================================
        // Delegate
        // ================================
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
}
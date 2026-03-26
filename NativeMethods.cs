using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MultiCompte
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hParent);
        [DllImport("user32.dll")] public static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll", EntryPoint = "GetWindowLongA")] public static extern int  GetWindowLong(IntPtr hWnd, int idx);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongA")] public static extern int  SetWindowLong(IntPtr hWnd, int idx, int val);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lp);
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc proc, IntPtr lp);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int n);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mod, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        // Hook souris global
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        public delegate bool   EnumWindowsProc(IntPtr hWnd, IntPtr lp);

        public const int WH_MOUSE_LL    = 14;
        public const int HC_ACTION      = 0;
        public const int WM_MBUTTONDOWN = 0x0207;
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public System.Drawing.Point pt;
            public uint mouseData, flags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public const int  GWL_STYLE        = -16;
        public const int  GWL_EXSTYLE      = -20;
        public const int  WS_CAPTION       = 0x00C00000;
        public const int  WS_CHILD         = 0x40000000;
        public const int  WS_POPUP         = unchecked((int)0x80000000);
        public const int  WS_OVERLAPPED    = 0x00000000;
        public const int  WS_BORDER        = 0x00800000;
        public const int  WS_THICKFRAME    = 0x00040000;
        public const int  WS_DLGFRAME      = 0x00400000;
        public const int  WS_SYSMENU       = 0x00080000;
        public const int  WS_MINIMIZEBOX   = 0x00020000;
        public const int  WS_MAXIMIZEBOX   = 0x00010000;
        public const int  WS_EX_DLGMODALFRAME = 0x00000001;
        public const int  WS_EX_CLIENTEDGE = 0x00000200;
        public const int  WS_EX_STATICEDGE = 0x00020000;
        public const int  WS_EX_WINDOWEDGE = 0x00000100;
        public const uint SWP_NOACT        = 0x0010;
        public const uint SWP_NOZORD       = 0x0004;
        public const uint SWP_NOMOVE       = 0x0002;
        public const uint SWP_NOSIZE       = 0x0001;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_SHOWWINDOW   = 0x0040;
        public const int  SW_HIDE          = 0;
        public const int  SW_SHOW          = 5;
        public const int  SW_RESTORE       = 9;
        public const uint WM_LBUTTONDOWN   = 0x0201;
        public const uint WM_LBUTTONUP     = 0x0202;

        public static void StripAllBorders(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            style &= ~WS_CAPTION & ~WS_BORDER & ~WS_THICKFRAME & ~WS_DLGFRAME
                   & ~WS_SYSMENU & ~WS_MINIMIZEBOX & ~WS_MAXIMIZEBOX & ~WS_POPUP;
            style |= WS_CHILD;
            SetWindowLong(hwnd, GWL_STYLE, style);
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            ex &= ~WS_EX_DLGMODALFRAME & ~WS_EX_CLIENTEDGE & ~WS_EX_STATICEDGE & ~WS_EX_WINDOWEDGE;
            SetWindowLong(hwnd, GWL_EXSTYLE, ex);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORD | SWP_NOACT | SWP_FRAMECHANGED);
        }

        public static void RestoreBorders(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            style &= ~WS_CHILD & ~WS_POPUP;
            style |= WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
            SetWindowLong(hwnd, GWL_STYLE, style);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORD | SWP_NOACT | SWP_FRAMECHANGED);
        }

        public static string GetWinText(IntPtr h)
        { var sb = new StringBuilder(512); GetWindowText(h, sb, sb.Capacity); return sb.ToString().Trim(); }

        public static string GetBestTitleForPid(int pid)
        {
            string best = string.Empty;
            EnumWindows((h, _) =>
            {
                if (!IsWindowVisible(h)) return true;
                GetWindowThreadProcessId(h, out uint wpid);
                if ((int)wpid != pid) return true;
                string t = GetWinText(h);
                if (string.IsNullOrWhiteSpace(t)) return true;
                if (Regex.IsMatch(t, @"(?i).+\s*[-|•]\s*dofus")) { best = t; return false; }
                if (best.Length < t.Length) best = t;
                return true;
            }, IntPtr.Zero);
            return best;
        }

        public static string GetBestChildTitle(IntPtr panel, int pid)
        {
            string best = string.Empty;
            EnumChildWindows(panel, (h, _) =>
            {
                string t = GetWinText(h);
                if (string.IsNullOrWhiteSpace(t)) return true;
                if (Regex.IsMatch(t, @"(?i).+\s*[-|•]\s*dofus")) { best = t; return false; }
                if (best.Length < t.Length) best = t;
                return true;
            }, IntPtr.Zero);
            return string.IsNullOrWhiteSpace(best) ? GetBestTitleForPid(pid) : best;
        }

        public static string ExtractPseudo(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "Connexion...";
            if (Regex.IsMatch(title.Trim(), @"^dofus(\s+\d+(\.\d+)*)?$", RegexOptions.IgnoreCase)) return "Connexion...";
            string c = Regex.Replace(title, @"(?i)\s*[-|•]\s*dofus(\s+\d+(\.\d+)*)?\s*.*$", "").Trim();
            if (!string.IsNullOrWhiteSpace(c) && !Regex.IsMatch(c, @"^dofus", RegexOptions.IgnoreCase)) return c;
            foreach (string p in title.Split(new[] { '-', '|', '•' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string s = p.Trim();
                if (s.Length <= 1 || Regex.IsMatch(s, @"^dofus", RegexOptions.IgnoreCase) || Regex.IsMatch(s, @"^v?\d")) continue;
                return s;
            }
            return "Connexion...";
        }
    }
}

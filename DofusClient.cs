using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MultiCompte
{
    internal class DofusClient
    {
        public Process Process  { get; }
        public int     Pid      => Process.Id;
        public bool    Embedded { get; private set; }
        private IntPtr _hostedHwnd = IntPtr.Zero;
        private Rectangle _viewport = Rectangle.Empty;

        public Panel Panel { get; }

        private string _pseudo = "Connexion...";
        public  string  Pseudo  => _pseudo;
        public  void    SetPseudo(string v) { _pseudo = v; }

        public DofusClient(Process process)
        {
            Process = process;
            Panel   = new Panel
            {
                BackColor = Color.Black,
                Margin    = Padding.Empty,
                Padding   = Padding.Empty,
                Dock      = DockStyle.Fill
            };
            Panel.SizeChanged += (_, __) => ForceResize();
        }

        public void Embed()
        {
            try
            {
                IntPtr hwnd = RefreshHwnd();
                if (hwnd == IntPtr.Zero) return;
                NativeMethods.StripAllBorders(hwnd);
                NativeMethods.SetParent(hwnd, Panel.Handle);
                Embedded = true;
                ForceResize();
            }
            catch { }
        }

        public void ForceResize()
        {
            if (!Embedded) return;
            try
            {
                IntPtr hwnd = RefreshHwnd();
                if (hwnd == IntPtr.Zero) return;

                NativeMethods.GetClientRect(Panel.Handle, out NativeMethods.RECT pr);
                int panelW = pr.Width;
                int panelH = pr.Height;
                if (panelW <= 0 || panelH <= 0) return;

                // Plein écran du container, identique sur toutes machines.
                int x = 0, y = 0;
                int w = panelW, h = panelH;
                NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h,
                    NativeMethods.SWP_NOZORD | NativeMethods.SWP_NOACT | NativeMethods.SWP_SHOWWINDOW);

                // Une seule compensation contrôlée si la zone cliente diffère.
                if (NativeMethods.GetClientRect(hwnd, out NativeMethods.RECT cr))
                {
                    int dw = panelW - cr.Width;
                    int dh = panelH - cr.Height;
                    // Garde-fou: évite les dérives DPI aberrantes.
                    if (Math.Abs(dw) <= 64 && Math.Abs(dh) <= 64)
                    {
                        w = Math.Max(1, w + dw);
                        h = Math.Max(1, h + dh);
                        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, x, y, w, h,
                            NativeMethods.SWP_NOZORD | NativeMethods.SWP_NOACT | NativeMethods.SWP_SHOWWINDOW);
                    }
                }

                _viewport = new Rectangle(0, 0, panelW, panelH);

                // Force aussi la surface de rendu interne (AIR/child window).
                NativeMethods.EnumChildWindows(hwnd, (child, _) =>
                {
                    NativeMethods.SetWindowPos(child, IntPtr.Zero, 0, 0, panelW, panelH,
                        NativeMethods.SWP_NOZORD | NativeMethods.SWP_NOACT | NativeMethods.SWP_SHOWWINDOW);
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        public void Detach()
        {
            try
            {
                IntPtr hwnd = RefreshHwnd();
                if (hwnd == IntPtr.Zero) return;
                NativeMethods.RestoreBorders(hwnd);
                NativeMethods.SetParent(hwnd, IntPtr.Zero);
                NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWLP_HWNDPARENT, IntPtr.Zero);
                NativeMethods.EnableWindow(hwnd, true);
                NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 120, 80, 1280, 800,
                    NativeMethods.SWP_NOZORD | NativeMethods.SWP_NOACT | NativeMethods.SWP_SHOWWINDOW | NativeMethods.SWP_FRAMECHANGED);
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(hwnd);
                Embedded = false;
                _viewport = Rectangle.Empty;
            }
            catch { }
        }

        public void Kill()
        {
            try
            {
                if (!Process.HasExited)
                {
                    Process.Kill(true);
                    Process.WaitForExit(1500);
                }
            }
            catch { }
            try
            {
                using var tk = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {Pid}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                tk?.WaitForExit(1500);
            }
            catch { }
            try
            {
                var p = Process.GetProcessById(Pid);
                if (!p.HasExited)
                {
                    p.Kill(true);
                    p.WaitForExit(1500);
                }
            }
            catch { }
            Embedded = false;
            _viewport = Rectangle.Empty;
        }

        public void BroadcastLeftClick(int localX, int localY)
        {
            try
            {
                IntPtr hwnd = RefreshHwnd();
                if (hwnd == IntPtr.Zero) return;
                Rectangle vp = _viewport;
                int x = Math.Max(0, localX - vp.X);
                int y = Math.Max(0, localY - vp.Y);

                IntPtr lp = (IntPtr)(((y & 0xFFFF) << 16) | (x & 0xFFFF));
                NativeMethods.SendMessage(hwnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)0x0001, lp);
                NativeMethods.SendMessage(hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lp);
            }
            catch { }
        }

        public void RefreshPseudo()
        {
            try
            {
                string title = NativeMethods.GetBestChildTitle(Panel.Handle, Pid);
                if (string.IsNullOrWhiteSpace(title) || title == "Connexion...")
                    title = NativeMethods.GetBestTitleForPid(Pid);
                string ps = NativeMethods.ExtractPseudo(title);
                if (ps != _pseudo) _pseudo = ps;
            }
            catch { }
        }

        public bool IsAlive()
        { try { return !Process.HasExited; } catch { return false; } }

        private IntPtr RefreshHwnd()
        {
            if (_hostedHwnd != IntPtr.Zero && NativeMethods.IsWindow(_hostedHwnd))
                return _hostedHwnd;
            try
            {
                var p = Process.GetProcessById(Pid);
                p.Refresh();
                _hostedHwnd = p.MainWindowHandle;
                return _hostedHwnd;
            }
            catch { return IntPtr.Zero; }
        }
    }
}

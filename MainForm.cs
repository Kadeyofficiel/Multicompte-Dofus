using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiCompte
{
    internal class MainForm : Form
    {
        private const int WM_HOTKEY   = 0x0312;
        private const int ID_SWITCH   = 1;
        private const int ID_CUSTOM   = 2;
        private const int TOP_H       = 34;
        private const int PERSO_H     = 34;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int RESIZE_BORDER = 8;

        private Color C_TOP   = Color.FromArgb(18, 22, 40);
        private Color C_PERSO = Color.FromArgb(13, 17, 32);
        private Color C_ACC   = Color.FromArgb(233, 69, 96);
        private Color C_BLUE  = Color.FromArgb(72, 120, 230);
        private Color C_GREEN = Color.FromArgb(46, 204, 113);
        private Color C_TEXT  = Color.FromArgb(210, 220, 240);
        private Color C_MUTED = Color.FromArgb(90, 105, 140);
        private Color C_HOVER = Color.FromArgb(30, 38, 62);
        private Color C_ACT   = Color.FromArgb(28, 40, 68);

        private readonly List<DofusClient>          _clients = new();
        private HashSet<int>?                       _selectedPids;
        private bool                                _loaded;
        private bool                                _middleActive;
        private int                                 _activeIdx = -1;
        private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 1500 };

        private IntPtr                           _hook = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc? _hookProc;

        private Button? _dragBtn;
        private int     _dragIdx;
        private Point   _dragStart;
        private bool    _isDragging;

        private readonly Panel  _topBar;
        private readonly Panel  _persoBar;
        private readonly Panel  _gameArea;
        private readonly Button _btnMiddle;
        private readonly Label  _titleLabel;

        private readonly ContextMenuStrip  _ctx;
        private DofusClient?               _ctxClient;
        private bool _shutdownStarted;
        private bool _allowClose;

        public MainForm()
        {
            SuspendLayout();
            Text = "abdefus";
            Size = new Size(Settings.WindowWidth, Settings.WindowHeight);
            MinimumSize = new Size(800, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black; ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 8.5f); DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = Settings.StartMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
            AutoScaleMode = AutoScaleMode.None;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var miRen = new ToolStripMenuItem("✏  Renommer")                         { ForeColor = C_TEXT };
            var miRet = new ToolStripMenuItem("⬅  Retirer (garde Dofus ouvert)")     { ForeColor = C_BLUE };
            var miFer = new ToolStripMenuItem("✕  Fermer ce client")                  { ForeColor = C_ACC  };
            miRen.Click += (_, __) => { if (_ctxClient == null) return; var n = InputBox("Renommer", "Nouveau nom :", _ctxClient.Pseudo); if (!string.IsNullOrWhiteSpace(n)) { _ctxClient.SetPseudo(n); BuildPersoBar(); } };
            miRet.Click += (_, __) => { if (_ctxClient != null) RemoveClient(_ctxClient, false); };
            miFer.Click += (_, __) => { if (_ctxClient != null) RemoveClient(_ctxClient, true); };
            _ctx = new ContextMenuStrip { BackColor = C_TOP, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9f) };
            _ctx.Items.Add(miRen); _ctx.Items.Add(new ToolStripSeparator()); _ctx.Items.Add(miRet); _ctx.Items.Add(miFer);

            _topBar = new Panel { Dock = DockStyle.Top, Height = TOP_H, BackColor = C_TOP };

            var logo = new PictureBox
            {
                Left = 8,
                Top = 5,
                Width = 22,
                Height = 22,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try
            {
                using var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null) logo.Image = appIcon.ToBitmap();
            }
            catch { }

            _titleLabel = new Label { Text = "abdefus", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = C_ACC, Left = 34, Top = 6, Height = 18, AutoSize = true, BackColor = Color.Transparent };

            _btnMiddle = Btn("Molette : OFF", 150, 150, Color.White);
            _btnMiddle.Click += (_, __) => ToggleHook();

            var bGerer   = Btn("⚙ Gérer",   306, 72, Color.White);   bGerer.Click   += (_, __) => ShowPicker();
            var bOptions = Btn("⚙ Options", 384, 84, Color.White);   bOptions.Click += (_, __) =>
            {
                using var f = new OptionsForm();
                f.ShowDialog(this);
                RegisterHotkeys();
                ApplyAppearanceFromSettings();
                if (_activeIdx >= 0 && _activeIdx < _clients.Count) _clients[_activeIdx].ForceResize();
            };
            var bAddPersos = Btn("+ Ajouter des persos", 474, 130, Color.White); bAddPersos.Click += (_, __) => ShowPicker();

            var bMin   = Btn("—", 0, 40, Color.White);
            var bMax   = Btn("⬜", 0, 40, Color.White);
            var bClose = Btn("✕", 0, 40, C_ACC);
            bMin.Anchor = bMax.Anchor = bClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _topBar.Layout += (_, __) => { bClose.Left = _topBar.Width-42; bMax.Left = _topBar.Width-84; bMin.Left = _topBar.Width-126; };
            bMin.Click   += (_, __) => WindowState = FormWindowState.Minimized;
            bMax.Click   += (_, __) => { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; };
            bClose.Click += (_, __) => Close();
            bClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 0, 0);

            _topBar.Controls.AddRange(new Control[] { logo, _titleLabel, _btnMiddle, bGerer, bOptions, bAddPersos, bMin, bMax, bClose });

            bool drag = false; Point dragPt = Point.Empty;
            _topBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { drag = true; dragPt = e.Location; } };
            _topBar.MouseMove += (s, e) => { if (drag) Location = new Point(Location.X+e.X-dragPt.X, Location.Y+e.Y-dragPt.Y); };
            _topBar.MouseUp   += (_, __) => drag = false;
            _titleLabel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { drag = true; dragPt = new Point(e.X + _titleLabel.Left, e.Y); } };
            _titleLabel.MouseMove += (s, e) => { if (drag) Location = new Point(Location.X + e.X + _titleLabel.Left - dragPt.X, Location.Y + e.Y - dragPt.Y); };
            _titleLabel.MouseUp   += (_, __) => drag = false;

            _persoBar = new Panel { Dock = DockStyle.Top, Height = PERSO_H, BackColor = C_PERSO };

            _gameArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Margin = Padding.Empty, Padding = Padding.Empty };

            _gameArea.SizeChanged += (_, __) =>
            {
                if (_activeIdx >= 0 && _activeIdx < _clients.Count)
                    _clients[_activeIdx].ForceResize();
            };

            Controls.Add(_gameArea);
            Controls.Add(_persoBar);
            Controls.Add(_topBar);

            Shown += MainForm_Shown;
            FormClosing += MainForm_FormClosing;
            _refreshTimer.Tick += RefreshTick;
            ApplyAppearanceFromSettings();
            UiScale.ApplyToForm(this);
            ResumeLayout(false);
        }

        private Button Btn(string t, int l, int w, Color fg)
        {
            var b = new Button { Text=t, Left=l, Top=1, Width=w, Height=TOP_H-2, FlatStyle=FlatStyle.Flat, BackColor=Color.Transparent, ForeColor=fg, Cursor=Cursors.Hand, Font=new Font("Segoe UI",8f), TextAlign=ContentAlignment.MiddleCenter };
            b.FlatAppearance.BorderSize=0; b.FlatAppearance.MouseOverBackColor=C_HOVER;
            return b;
        }

        private void ApplyAppearanceFromSettings()
        {
            ApplyCustomColorsFromSettings();
            if (Settings.StartMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                if (WindowState == FormWindowState.Maximized)
                    WindowState = FormWindowState.Normal;
                Size = new Size(
                    Math.Max(MinimumSize.Width, Settings.WindowWidth),
                    Math.Max(MinimumSize.Height, Settings.WindowHeight));
            }
            BuildPersoBar();
            Invalidate(true);
        }

        private void ApplyCustomColorsFromSettings()
        {
            ApplyPreviewPalette(
                Settings.TopBarColor,
                Settings.PersoBarColor,
                Settings.AccentColor,
                Settings.TextColor);
        }

        public void ApplyPreviewPalette(Color top, Color perso, Color accent, Color text)
        {
            C_TOP = top;
            C_PERSO = perso;
            C_ACC = accent;
            C_BLUE = accent;
            C_GREEN = accent;
            C_TEXT = text;
            C_MUTED = Blend(text, top, 0.50f);
            C_HOVER = Blend(top, Color.White, 0.12f);
            C_ACT = Blend(perso, accent, 0.28f);

            var topText = EnsureReadableText(C_TEXT, C_TOP);
            var persoText = EnsureReadableText(C_TEXT, C_PERSO);

            BackColor = Color.Black;
            ForeColor = topText;
            _topBar.BackColor = C_TOP;
            _persoBar.BackColor = C_PERSO;
            _ctx.BackColor = C_TOP;
            _ctx.ForeColor = topText;

            // Applique un style lisible aux éléments de la top bar.
            foreach (Control ctl in _topBar.Controls)
            {
                if (ctl is Button b)
                {
                    if (b.Text == "✕")
                    {
                        b.ForeColor = Color.White;
                        b.FlatAppearance.BorderSize = 0;
                        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 0, 0);
                    }
                    else
                    {
                        b.ForeColor = topText;
                        b.FlatAppearance.BorderSize = 0;
                        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(95, accent.R, accent.G, accent.B);
                    }
                }
                else if (ctl is Label l)
                {
                    if (ReferenceEquals(l, _titleLabel))
                        l.ForeColor = accent;
                    else
                        l.ForeColor = topText;
                }
            }

            // Assure une bonne lisibilité de la barre des persos.
            _persoBar.ForeColor = persoText;
        }

        private static Color Blend(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            int r = (int)Math.Round(a.R + (b.R - a.R) * t);
            int g = (int)Math.Round(a.G + (b.G - a.G) * t);
            int bl = (int)Math.Round(a.B + (b.B - a.B) * t);
            return Color.FromArgb(
                Math.Clamp(r, 0, 255),
                Math.Clamp(g, 0, 255),
                Math.Clamp(bl, 0, 255));
        }

        private static Color EnsureReadableText(Color candidate, Color background)
        {
            double c = ContrastRatio(candidate, background);
            if (c >= 3.2) return candidate;
            var white = Color.White;
            var black = Color.Black;
            return ContrastRatio(white, background) >= ContrastRatio(black, background) ? white : black;
        }

        private static double ContrastRatio(Color a, Color b)
        {
            double l1 = RelativeLuminance(a);
            double l2 = RelativeLuminance(b);
            double hi = Math.Max(l1, l2);
            double lo = Math.Min(l1, l2);
            return (hi + 0.05) / (lo + 0.05);
        }

        private static double RelativeLuminance(Color c)
        {
            static double C(double v)
            {
                v /= 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * C(c.R) + 0.7152 * C(c.G) + 0.0722 * C(c.B);
        }

        private void ToggleHook()
        {
            _middleActive = !_middleActive;
            if (_middleActive)
            {
                _hookProc = HookCallback;
                using var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
                _hook = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_MOUSE_LL, _hookProc,
                    NativeMethods.GetModuleHandle(mod.ModuleName), 0);
                _btnMiddle.Text = "Molette : ON"; _btnMiddle.ForeColor = Color.White;
                _btnMiddle.BackColor = Color.FromArgb(180, 120, 255, 170);
            }
            else
            {
                if (_hook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
                _btnMiddle.Text = "Molette : OFF"; _btnMiddle.ForeColor = Color.White; _btnMiddle.BackColor = Color.Transparent;
            }
            BuildPersoBar();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode == NativeMethods.HC_ACTION
                && (int)wParam == NativeMethods.WM_MBUTTONDOWN
                && _middleActive && _clients.Count > 0)
            {
                var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

                var ga = _gameArea.RectangleToScreen(new System.Drawing.Rectangle(0, 0, _gameArea.Width, _gameArea.Height));
                if (ga.Contains(info.pt))
                {
                    int lx = info.pt.X - ga.Left;
                    int ly = info.pt.Y - ga.Top;

                    foreach (var c in _clients.ToList())
                        c.BroadcastLeftClick(lx, ly);

                    return (IntPtr)1;
                }
            }
            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            if (_loaded) return;
            _loaded = true;

            if (Settings.RememberEnabled)
            {
                var names = Settings.RememberedNames;
                if (names.Count > 0)
                {
                    _selectedPids = new HashSet<int>();
                    foreach (var p in Process.GetProcessesByName("Dofus"))
                    {
                        try
                        {
                            p.Refresh();
                            if (p.MainWindowHandle == IntPtr.Zero || p.HasExited) continue;
                            string ps = NativeMethods.ExtractPseudo(NativeMethods.GetBestTitleForPid(p.Id));
                            if (names.Contains(ps)) _selectedPids.Add(p.Id);
                        }
                        catch { }
                    }
                    if (_selectedPids.Count > 0) { LoadClients(); _refreshTimer.Start(); RegisterHotkeys(); return; }
                }
            }
            ShowPicker();
        }

        private void ShowPicker()
        {
            using var pk = new PickerForm();
            _selectedPids = pk.ShowDialog(this) == DialogResult.OK ? pk.SelectedPids : null;
            LoadClients();
            _refreshTimer.Start();
            RegisterHotkeys();
        }

        private void RegisterHotkeys()
        {
            NativeMethods.UnregisterHotKey(Handle, ID_SWITCH);
            NativeMethods.UnregisterHotKey(Handle, ID_CUSTOM);

            var sw = OptionsForm.LoadSwitch();
            if (sw.IsValid)
                NativeMethods.RegisterHotKey(Handle, ID_SWITCH, sw.Modifiers, sw.Vk);

            var cu = OptionsForm.LoadCustom();
            if (cu.IsValid)
                NativeMethods.RegisterHotKey(Handle, ID_CUSTOM, cu.Modifiers, cu.Vk);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    Point p = PointToClient(Cursor.Position);
                    bool left = p.X <= RESIZE_BORDER;
                    bool right = p.X >= ClientSize.Width - RESIZE_BORDER;
                    bool top = p.Y <= RESIZE_BORDER;
                    bool bottom = p.Y >= ClientSize.Height - RESIZE_BORDER;

                    if (left && top) m.Result = (IntPtr)HTTOPLEFT;
                    else if (right && top) m.Result = (IntPtr)HTTOPRIGHT;
                    else if (left && bottom) m.Result = (IntPtr)HTBOTTOMLEFT;
                    else if (right && bottom) m.Result = (IntPtr)HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)HTLEFT;
                    else if (right) m.Result = (IntPtr)HTRIGHT;
                    else if (top) m.Result = (IntPtr)HTTOP;
                    else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                }
                return;
            }

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == ID_SWITCH && _clients.Count > 1)
                    SwitchTo((_activeIdx + 1) % _clients.Count);
                else if (id == ID_CUSTOM)
                {
                    var cu = OptionsForm.LoadCustom();
                    if (!string.IsNullOrWhiteSpace(cu.Action))
                        SendKeys.Send(cu.Action);
                }
            }
            base.WndProc(ref m);
        }

        private void LoadClients()
        {
            foreach (var c in _clients.ToList())
                if (!c.IsAlive()) RemoveClient(c, false);

            foreach (Process p in Process.GetProcessesByName("Dofus"))
            {
                try
                {
                    p.Refresh();
                    if (p.HasExited || p.MainWindowHandle == IntPtr.Zero) continue;
                    if (_clients.Any(c => c.Pid == p.Id)) continue;
                    if (_selectedPids != null && !_selectedPids.Contains(p.Id)) continue;
                    var cl = new DofusClient(p);
                    cl.RefreshPseudo();
                    _clients.Add(cl);
                }
                catch { }
            }

            if (_activeIdx < 0 && _clients.Count > 0) _activeIdx = 0;
            ShowActive();
            BuildPersoBar();
        }

        private void RemoveClient(DofusClient cl, bool kill)
        {
            int idx = _clients.IndexOf(cl);
            if (kill) cl.Kill(); else cl.Detach();
            if (_gameArea.Controls.Contains(cl.Panel))
                _gameArea.Controls.Remove(cl.Panel);
            cl.Panel.Dispose();
            _clients.Remove(cl);
            if (_selectedPids != null) _selectedPids.Remove(cl.Pid);

            if (_clients.Count == 0)
            {
                _activeIdx = -1;
                _gameArea.Controls.Clear();
                BuildPersoBar();
                return;
            }

            if (idx <= _activeIdx && _activeIdx > 0)
                _activeIdx--;
            if (_activeIdx >= _clients.Count)
                _activeIdx = _clients.Count - 1;

            ActivatePanel(_activeIdx);
            BuildPersoBar();
        }

        private void ShowActive()
        {
            if (_activeIdx < 0 || _activeIdx >= _clients.Count)
            { _gameArea.Controls.Clear(); return; }

            foreach (var c in _clients)
            {
                if (!_gameArea.Controls.Contains(c.Panel))
                    _gameArea.Controls.Add(c.Panel);

                if (!c.Embedded && c.Panel.IsHandleCreated)
                    c.Embed();
                else if (!c.Embedded)
                    c.Panel.HandleCreated += OnPanelCreated;
            }

            ActivatePanel(_activeIdx);
        }

        private void ActivatePanel(int idx)
        {
            for (int i = 0; i < _clients.Count; i++)
                _clients[i].Panel.Visible = (i == idx);

            if (idx >= 0 && idx < _clients.Count)
            {
                _clients[idx].Panel.BringToFront();
                _clients[idx].ForceResize();
            }
        }

        private void OnPanelCreated(object? sender, EventArgs e)
        {
            if (sender is Panel p)
            {
                p.HandleCreated -= OnPanelCreated;
                var cl = _clients.FirstOrDefault(c => c.Panel == p);
                if (cl != null && !cl.Embedded)
                {
                    cl.Embed();
                    if (_clients.IndexOf(cl) != _activeIdx)
                        cl.Panel.Visible = false;
                }
            }
        }

        private void SwitchTo(int idx)
        {
            if (idx < 0 || idx >= _clients.Count || idx == _activeIdx) return;
            _activeIdx = idx;
            ActivatePanel(idx);
            BuildPersoBar();
        }

        private void BuildPersoBar()
        {
            if (InvokeRequired) { Invoke(new Action(BuildPersoBar)); return; }
            _persoBar.Controls.Clear();
            int x = 4;

            for (int i = 0; i < _clients.Count; i++)
            {
                var  cl     = _clients[i];
                bool active = i == _activeIdx;
                int  ci     = i;

                var btn = new Button
                {
                    Tag = cl, Text = _middleActive && active ? $"  ON  {cl.Pseudo}" : $"  {cl.Pseudo}", Left = x, Top = 3,
                    Height = PERSO_H-6, FlatStyle = FlatStyle.Flat,
                    BackColor = active ? C_ACT : Color.Transparent,
                    ForeColor = (_middleActive && active) ? Color.FromArgb(140, 255, 190) : (active ? C_TEXT : C_MUTED), Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 8.5f, active ? FontStyle.Bold : FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                btn.FlatAppearance.BorderSize  = 1;
                btn.FlatAppearance.BorderColor = active ? C_ACC : C_PERSO;
                btn.FlatAppearance.MouseOverBackColor = C_HOVER;
                btn.Width = TextRenderer.MeasureText(btn.Text, btn.Font).Width + 28;

                btn.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(new SolidBrush(active ? C_GREEN : Color.FromArgb(55, C_GREEN)), 7, (btn.Height-7)/2, 7, 7);
                };

                btn.Click     += (_, __) => SwitchTo(ci);
                btn.MouseDown += (s, e) => { if (e.Button == MouseButtons.Right) { _ctxClient = cl; _ctx.Show(btn, e.Location); } };

                AttachDrag(btn, ci);
                _persoBar.Controls.Add(btn);
                x += btn.Width + 3;
            }

            // Bouton déplacé dans la top bar: "+ Ajouter des persos".
        }

        private void AttachDrag(Button btn, int idx)
        {
            btn.MouseDown += (s, e) => { if (e.Button != MouseButtons.Left) return; _dragBtn=btn; _dragIdx=idx; _dragStart=e.Location; _isDragging=false; };
            btn.MouseMove += (s, e) =>
            {
                if (_dragBtn!=btn || e.Button!=MouseButtons.Left) return;
                if (!_isDragging) { if (Math.Abs(e.X-_dragStart.X)<5) return; _isDragging=true; btn.Cursor=Cursors.SizeWE; }
                var pt = _persoBar.PointToClient(btn.PointToScreen(e.Location));
                foreach (Control ctrl in _persoBar.Controls)
                {
                    if (ctrl is Button ob && ob.Tag is DofusClient dc && ctrl.Bounds.Contains(pt))
                    {
                        int hi = _clients.IndexOf(dc);
                        if (hi < 0 || hi == _dragIdx) break;
                        (_clients[_dragIdx], _clients[hi]) = (_clients[hi], _clients[_dragIdx]);
                        if (_activeIdx == _dragIdx) _activeIdx = hi;
                        else if (_activeIdx == hi) _activeIdx = _dragIdx;
                        _dragIdx = hi; BuildPersoBar(); break;
                    }
                }
            };
            btn.MouseUp += (s, e) => { if (_dragBtn!=btn) return; bool was=_isDragging; btn.Cursor=Cursors.Hand; _isDragging=false; _dragBtn=null; if (!was) SwitchTo(idx); };
        }

        private void RefreshTick(object? sender, EventArgs e)
        {
            bool changed = false;
            foreach (var c in _clients.ToList())
            {
                if (!c.IsAlive())
                {
                    int i = _clients.IndexOf(c); _clients.Remove(c);
                    if (_activeIdx >= i) _activeIdx = Math.Max(0, _activeIdx-1);
                    if (_clients.Count == 0) _activeIdx = -1;
                    changed = true;
                }
                else { string old=c.Pseudo; c.RefreshPseudo(); if (c.Pseudo!=old) changed=true; }
            }
            if (changed) { ShowActive(); BuildPersoBar(); }
        }

        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_allowClose) return;
            e.Cancel = true;
            await BeginShutdownAsync();
        }

        private async Task BeginShutdownAsync()
        {
            if (_shutdownStarted) return;
            _shutdownStarted = true;

            // Rend la fermeture visuellement fluide, puis termine en arrière-plan.
            Enabled = false;
            ShowInTaskbar = false;
            Opacity = 0;

            // Partie UI: toujours sur le thread UI (évite les exceptions inter-threads).
            _refreshTimer.Stop();
            if (_hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
            NativeMethods.UnregisterHotKey(Handle, ID_SWITCH);
            NativeMethods.UnregisterHotKey(Handle, ID_CUSTOM);

            var managedPids = _clients.Select(c => c.Pid).Distinct().ToList();
            _clients.Clear();

            // Partie process kill: en arrière-plan.
            await Task.Run(() => KillManagedProcesses(managedPids));

            _allowClose = true;
            Close();
        }

        private static void KillManagedProcesses(IEnumerable<int> managedPids)
        {
            // Ultime sécurité ciblée: ne force QUE les PID gérés par MultiCompte.
            foreach (int pid in managedPids)
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    if (!p.HasExited)
                    {
                        p.Kill(true);
                        p.WaitForExit(1500);
                    }
                }
                catch { }
                try
                {
                    using var tk = Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/T /F /PID {pid}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    tk?.WaitForExit(2000);
                }
                catch { }
            }
        }

        private void LancerDofus()
        {
            string path = Settings.DofusPath;
            if (!File.Exists(path))
            {
                using var dlg = new OpenFileDialog { Title = "Sélectionner Dofus.exe", Filter = "Dofus.exe|Dofus.exe|*.exe|*.exe" };
                if (dlg.ShowDialog() != DialogResult.OK) return;
                Settings.DofusPath = path = dlg.FileName;
            }
            try { System.Diagnostics.Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Erreur : " + ex.Message); }
        }

        private static string? InputBox(string title, string prompt, string def="")
        {
            var f=new Form{Text=title,Size=new Size(370,138),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(18,22,40)};
            var l=new Label{Text=prompt,Left=12,Top=12,Width=340,ForeColor=Color.FromArgb(210,220,240)};
            var t=new TextBox{Left=12,Top=32,Width=340,Text=def,BackColor=Color.FromArgb(22,28,50),ForeColor=Color.FromArgb(210,220,240),BorderStyle=BorderStyle.FixedSingle};
            var b=new Button{Text="OK",Left=264,Top=62,Width=88,Height=28,DialogResult=DialogResult.OK,BackColor=Color.FromArgb(233,69,96),ForeColor=Color.White,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",9f,FontStyle.Bold)};
            b.FlatAppearance.BorderSize=0; f.Controls.AddRange(new Control[]{l,t,b}); f.AcceptButton=b;
            return f.ShowDialog()==DialogResult.OK?t.Text.Trim():null;
        }
    }
}

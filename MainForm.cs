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
        private const int TOP_H       = 30;
        private const int PERSO_H     = 34;

        private static readonly Color C_TOP   = Color.FromArgb(18, 22, 40);
        private static readonly Color C_PERSO = Color.FromArgb(13, 17, 32);
        private static readonly Color C_ACC   = Color.FromArgb(233, 69, 96);
        private static readonly Color C_BLUE  = Color.FromArgb(72, 120, 230);
        private static readonly Color C_GREEN = Color.FromArgb(46, 204, 113);
        private static readonly Color C_TEXT  = Color.FromArgb(210, 220, 240);
        private static readonly Color C_MUTED = Color.FromArgb(90, 105, 140);
        private static readonly Color C_HOVER = Color.FromArgb(30, 38, 62);
        private static readonly Color C_ACT   = Color.FromArgb(28, 40, 68);

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

        private readonly ContextMenuStrip  _ctx;
        private DofusClient?               _ctxClient;
        private bool _shutdownStarted;
        private bool _allowClose;

        public MainForm()
        {
            SuspendLayout();
            Text = "MultiCompte"; Size = new Size(1366, 800); MinimumSize = new Size(800, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.Black; ForeColor = C_TEXT;
            Font = new Font("Segoe UI", 8.5f); DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            AutoScaleMode = AutoScaleMode.None;

            var miRen = new ToolStripMenuItem("✏  Renommer")                         { ForeColor = C_TEXT };
            var miRet = new ToolStripMenuItem("⬅  Retirer (garde Dofus ouvert)")     { ForeColor = C_BLUE };
            var miFer = new ToolStripMenuItem("✕  Fermer ce client")                  { ForeColor = C_ACC  };
            miRen.Click += (_, __) => { if (_ctxClient == null) return; var n = InputBox("Renommer", "Nouveau nom :", _ctxClient.Pseudo); if (!string.IsNullOrWhiteSpace(n)) { _ctxClient.SetPseudo(n); BuildPersoBar(); } };
            miRet.Click += (_, __) => { if (_ctxClient != null) RemoveClient(_ctxClient, false); };
            miFer.Click += (_, __) => { if (_ctxClient != null) RemoveClient(_ctxClient, true); };
            _ctx = new ContextMenuStrip { BackColor = C_TOP, ForeColor = C_TEXT, Font = new Font("Segoe UI", 9f) };
            _ctx.Items.Add(miRen); _ctx.Items.Add(new ToolStripSeparator()); _ctx.Items.Add(miRet); _ctx.Items.Add(miFer);

            _topBar = new Panel { Dock = DockStyle.Top, Height = TOP_H, BackColor = C_TOP };

            var lbl = new Label { Text = "🎮  MultiCompte", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = C_ACC, Left = 8, Top = 6, Width = 140, Height = 18, AutoSize = false };

            _btnMiddle = Btn("🖱 Molette : OFF", 150, 150, Color.White);
            _btnMiddle.Click += (_, __) => ToggleHook();

            var bGerer   = Btn("⚙ Gérer",   306, 72, Color.White);   bGerer.Click   += (_, __) => ShowPicker();
            var bOptions = Btn("⚙ Options", 384, 84, Color.White);   bOptions.Click += (_, __) => { using var f = new OptionsForm(); f.ShowDialog(this); RegisterHotkeys(); if (_activeIdx >= 0 && _activeIdx < _clients.Count) _clients[_activeIdx].ForceResize(); };
            var bAddPersos = Btn("+ Ajouter des persos", 474, 130, Color.White); bAddPersos.Click += (_, __) => ShowPicker();

            var bMin   = Btn("—", 0, 28, Color.White);
            var bMax   = Btn("⬜", 0, 28, Color.White);
            var bClose = Btn("✕", 0, 28, C_ACC);
            bMin.Anchor = bMax.Anchor = bClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _topBar.Layout += (_, __) => { bClose.Left = _topBar.Width-30; bMax.Left = _topBar.Width-60; bMin.Left = _topBar.Width-90; };
            bMin.Click   += (_, __) => WindowState = FormWindowState.Minimized;
            bMax.Click   += (_, __) => { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; };
            bClose.Click += (_, __) => Close();
            bClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 0, 0);

            _topBar.Controls.AddRange(new Control[] { lbl, _btnMiddle, bGerer, bOptions, bAddPersos, bMin, bMax, bClose });

            bool drag = false; Point dragPt = Point.Empty;
            _topBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { drag = true; dragPt = e.Location; } };
            _topBar.MouseMove += (s, e) => { if (drag) Location = new Point(Location.X+e.X-dragPt.X, Location.Y+e.Y-dragPt.Y); };
            _topBar.MouseUp   += (_, __) => drag = false;
            lbl.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { drag = true; dragPt = new Point(e.X+lbl.Left, e.Y); } };
            lbl.MouseMove += (s, e) => { if (drag) Location = new Point(Location.X+e.X+lbl.Left-dragPt.X, Location.Y+e.Y-dragPt.Y); };
            lbl.MouseUp   += (_, __) => drag = false;

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
            ResumeLayout(false);
        }

        private Button Btn(string t, int l, int w, Color fg)
        {
            var b = new Button { Text=t, Left=l, Top=1, Width=w, Height=TOP_H-2, FlatStyle=FlatStyle.Flat, BackColor=Color.Transparent, ForeColor=fg, Cursor=Cursors.Hand, Font=new Font("Segoe UI",8f), TextAlign=ContentAlignment.MiddleCenter };
            b.FlatAppearance.BorderSize=0; b.FlatAppearance.MouseOverBackColor=C_HOVER;
            return b;
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
                _btnMiddle.Text = "🖱 Molette : ON "; _btnMiddle.ForeColor = Color.White;
                _btnMiddle.BackColor = Color.FromArgb(180, 120, 255, 170);
            }
            else
            {
                if (_hook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
                _btnMiddle.Text = "🖱 Molette : OFF"; _btnMiddle.ForeColor = Color.White; _btnMiddle.BackColor = Color.Transparent;
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
                    Tag = cl, Text = _middleActive && active ? $"  🖱 {cl.Pseudo}" : $"  {cl.Pseudo}", Left = x, Top = 3,
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

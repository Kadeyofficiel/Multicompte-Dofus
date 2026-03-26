using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MultiCompte
{
    internal class PickerForm : Form
    {
        public HashSet<int>? SelectedPids { get; private set; }

        private List<(int Pid, string Pseudo)> _entries = new();
        private readonly CheckedListBox _list;
        private readonly CheckBox       _chkRemember;
        private readonly Label          _lblInfo;

        public PickerForm()
        {
            Text = "Gestion des pages — Clients Dofus";
            Size = new Size(500, 420); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            BackColor = Color.FromArgb(18, 22, 40); ForeColor = Color.FromArgb(210, 220, 240);
            Font = new Font("Segoe UI", 9.5f);

            Controls.Add(new Label { Text = "🎮  Clients Dofus détectés", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(233, 69, 96), Left = 14, Top = 12, Width = 460, Height = 26, AutoSize = false });
            _lblInfo = new Label { Left = 14, Top = 42, Width = 460, Height = 18, ForeColor = Color.FromArgb(90, 105, 140), AutoSize = false };
            Controls.Add(_lblInfo);

            _list = new CheckedListBox { Left = 14, Top = 66, Width = 460, Height = 246, CheckOnClick = true, BackColor = Color.FromArgb(22, 28, 50), ForeColor = Color.FromArgb(210, 220, 240), Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false };
            Controls.Add(_list);

            _chkRemember = new CheckBox { Left = 14, Top = 320, Width = 460, Text = "Mémoriser cette sélection", ForeColor = Color.FromArgb(90, 105, 140), Checked = Settings.RememberEnabled };
            Controls.Add(_chkRemember);

            Button Mk(string t, int l, Color bg) { var b = new Button { Text = t, Left = l, Top = 350, Width = 108, Height = 30, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
            var bAll = Mk("Tout cocher",   14,  Color.FromArgb(15, 52, 96));
            var bNone= Mk("Tout décocher", 128, Color.FromArgb(15, 52, 96));
            var bRef = Mk("↻ Rafraîchir",  242, Color.FromArgb(15, 52, 96));
            var bOk  = Mk("✔  Valider",    370, Color.FromArgb(233, 69, 96));
            bAll.Click  += (_, __) => { for (int i = 0; i < _list.Items.Count; i++) _list.SetItemChecked(i, true); };
            bNone.Click += (_, __) => { for (int i = 0; i < _list.Items.Count; i++) _list.SetItemChecked(i, false); };
            bRef.Click  += (_, __) => LoadList();
            bOk.Click   += Validate;
            Controls.AddRange(new Control[] { bAll, bNone, bRef, bOk });
            LoadList();
        }

        private void LoadList()
        {
            _list.Items.Clear(); _entries.Clear();
            var mem = Settings.RememberedNames;
            bool useMem = Settings.RememberEnabled && mem.Count > 0;

            foreach (Process p in Process.GetProcessesByName("Dofus"))
            {
                try
                {
                    p.Refresh();
                    if (p.HasExited || p.MainWindowHandle == IntPtr.Zero) continue;
                    string title  = NativeMethods.GetBestTitleForPid(p.Id);
                    if (string.IsNullOrWhiteSpace(title)) title = p.MainWindowTitle;
                    string pseudo = NativeMethods.ExtractPseudo(title);
                    int    idx    = _list.Items.Add($"{pseudo}   —   PID {p.Id}");
                    _list.SetItemChecked(idx, useMem ? mem.Contains(pseudo) : true);
                    _entries.Add((p.Id, pseudo));
                }
                catch { }
            }
            _lblInfo.Text = _list.Items.Count == 0
                ? "⚠  Aucun client Dofus. Lance Dofus puis ↻ Rafraîchir."
                : $"{_list.Items.Count} client(s) détecté(s). Coche ceux à intégrer.";
        }

        private void Validate(object? sender, EventArgs e)
        {
            SelectedPids = new HashSet<int>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _list.Items.Count; i++)
                if (_list.GetItemChecked(i)) { SelectedPids.Add(_entries[i].Pid); names.Add(_entries[i].Pseudo); }
            Settings.RememberEnabled = _chkRemember.Checked;
            Settings.RememberedNames = _chkRemember.Checked ? names : new HashSet<string>();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

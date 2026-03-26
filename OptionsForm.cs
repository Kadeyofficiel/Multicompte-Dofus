using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MultiCompte
{
    /// <summary>
    /// Fenêtre Options — raccourcis configurables
    /// </summary>
    internal class OptionsForm : Form
    {
        private static readonly Color C_BG   = Color.FromArgb(18, 22, 40);
        private static readonly Color C_CARD = Color.FromArgb(24, 30, 52);
        private static readonly Color C_TEXT = Color.FromArgb(210, 220, 240);
        private static readonly Color C_MUTED= Color.FromArgb(90, 105, 140);
        private static readonly Color C_ACC  = Color.FromArgb(233, 69, 96);

        // ── Raccourci 1 : changer de personnage (switch) ──────────────
        private readonly CheckBox _chkSwAlt, _chkSwCtrl, _chkSwWin;
        private readonly ComboBox _cmbSwKey;

        // ── Raccourci 2 : personnalisé libre ──────────────────────────
        private readonly CheckBox _chkCustAlt, _chkCustCtrl, _chkCustWin;
        private readonly ComboBox _cmbCustKey;
        private readonly TextBox  _txtCustAction;
        private readonly ComboBox _cmbPreset;
        private readonly Panel _pTopBarColor, _pPersoBarColor, _pAccentColor, _pTextColor;
        private readonly CheckBox _chkStartMax;
        private readonly NumericUpDown _numWinW, _numWinH;

        public OptionsForm()
        {
            Text            = "Options — Raccourcis et apparence";
            Size            = new Size(520, 560);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            BackColor       = C_BG;
            ForeColor       = C_TEXT;
            Font            = new Font("Segoe UI", 9f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            // ── Titre ──────────────────────────────────────────────────
            var lbl = new Label
            {
                Text = "⚙  Options — Raccourcis clavier",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = C_ACC, Left = 14, Top = 12, Width = 480, Height = 24, AutoSize = false
            };

            // ══════════════════════════════════════════════════════════
            // SECTION 1 : Raccourci Switch perso
            // ══════════════════════════════════════════════════════════
            var sec1 = MakeSection("Raccourci : changer de personnage actif", 44);

            _chkSwAlt  = MakeCheck("Alt",    14, 94);
            _chkSwCtrl = MakeCheck("Ctrl",   64, 94);
            _chkSwWin  = MakeCheck("Win",   114, 94);
            var lblSwPlus = new Label { Text = "+", Left = 162, Top = 96, Width = 14, ForeColor = C_MUTED };
            _cmbSwKey  = MakeCombo(178, 94);

            var lblSwInfo = new Label
            {
                Text = "Ce raccourci switche vers le personnage suivant dans la liste.",
                ForeColor = C_MUTED, Left = 14, Top = 122, Width = 480, Height = 16, AutoSize = false
            };

            // ══════════════════════════════════════════════════════════
            // SECTION 2 : Raccourci personnalisé
            // ══════════════════════════════════════════════════════════
            var sec2 = MakeSection("Raccourci personnalisé (ex: ouvrir une appli, envoyer une touche)", 152);

            _chkCustAlt  = MakeCheck("Alt",    14, 202);
            _chkCustCtrl = MakeCheck("Ctrl",   64, 202);
            _chkCustWin  = MakeCheck("Win",   114, 202);
            var lblCustPlus = new Label { Text = "+", Left = 162, Top = 204, Width = 14, ForeColor = C_MUTED };
            _cmbCustKey  = MakeCombo(178, 202);

            var lblCustAction = new Label { Text = "Action (touche à envoyer à tous les clients) :", Left = 14, Top = 232, Width = 300, ForeColor = C_MUTED };
            _txtCustAction = new TextBox
            {
                Left = 14, Top = 250, Width = 200, Height = 24,
                BackColor = C_CARD, ForeColor = C_TEXT, BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "ex: F1, Escape, Tab..."
            };

            var sec3 = MakeSection("Apparence et taille de fenêtre", 284);
            var lblPreset = new Label { Text = "Preset", Left = 14, Top = 312, Width = 50, ForeColor = C_TEXT };
            _cmbPreset = new ComboBox
            {
                Left = 66, Top = 308, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = C_CARD, ForeColor = C_TEXT
            };
            _cmbPreset.Items.AddRange(new object[] {
                "Océan", "Sakura", "Émeraude", "Violet", "Noir pro",
                "Naruto", "Dragon Ball", "Cyberpunk", "Lave", "Glacier", "Soleil", "Forêt"
            });
            _cmbPreset.SelectedIndex = 0;
            var bPreset = new Button
            {
                Text = "Appliquer",
                Left = 212, Top = 307, Width = 88, Height = 24,
                BackColor = C_CARD, ForeColor = C_TEXT, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            bPreset.FlatAppearance.BorderSize = 1;
            bPreset.FlatAppearance.BorderColor = C_MUTED;
            bPreset.Click += (_, __) =>
            {
                ApplyPreset(_cmbPreset.SelectedItem?.ToString() ?? "Océan");
                ApplyLivePreview();
            };

            var lblColors = new Label { Text = "Couleurs (appliquées en live)", Left = 14, Top = 334, Width = 220, ForeColor = C_TEXT };

            var lblTop = new Label { Text = "Barre haut", Left = 14, Top = 342, Width = 70, ForeColor = C_TEXT };
            _pTopBarColor = MakeColorPanel(88, 339);
            _pTopBarColor.Click += (_, __) => PickColor(_pTopBarColor);

            var lblPerso = new Label { Text = "Barre persos", Left = 150, Top = 342, Width = 78, ForeColor = C_TEXT };
            _pPersoBarColor = MakeColorPanel(232, 339);
            _pPersoBarColor.Click += (_, __) => PickColor(_pPersoBarColor);

            var lblAccent = new Label { Text = "Accent", Left = 294, Top = 342, Width = 48, ForeColor = C_TEXT };
            _pAccentColor = MakeColorPanel(346, 339);
            _pAccentColor.Click += (_, __) => PickColor(_pAccentColor);

            var lblText = new Label { Text = "Texte", Left = 408, Top = 342, Width = 40, ForeColor = C_TEXT };
            _pTextColor = MakeColorPanel(450, 339);
            _pTextColor.Click += (_, __) => PickColor(_pTextColor);

            _chkStartMax = new CheckBox
            {
                Text = "Ouvrir en plein écran",
                Left = 14,
                Top = 374,
                Width = 210,
                ForeColor = C_TEXT,
                BackColor = Color.Transparent
            };

            var lblSize = new Label { Text = "Taille fenêtre", Left = 14, Top = 406, Width = 80, ForeColor = C_TEXT };
            _numWinW = MakeNum(90, 404, 800, 3840, 1366);
            var lblX = new Label { Text = "x", Left = 174, Top = 408, Width = 14, ForeColor = C_MUTED };
            _numWinH = MakeNum(190, 404, 500, 2160, 800);
            var lblSizeInfo = new Label
            {
                Text = "Appliquée quand le mode plein écran est désactivé.",
                Left = 260, Top = 406, Width = 220, Height = 16, ForeColor = C_MUTED
            };

            var btnSave = new Button
            {
                Text = "💾  Sauvegarder", Left = 322, Top = 476, Width = 160, Height = 30,
                BackColor = C_ACC, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += Save;

            var btnCancel = new Button
            {
                Text = "Annuler", Left = 234, Top = 476, Width = 82, Height = 30,
                BackColor = C_CARD, ForeColor = C_MUTED, FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (_, __) => Close();

            Controls.AddRange(new Control[] {
                lbl,
                sec1, _chkSwAlt, _chkSwCtrl, _chkSwWin, lblSwPlus, _cmbSwKey, lblSwInfo,
                sec2, _chkCustAlt, _chkCustCtrl, _chkCustWin, lblCustPlus, _cmbCustKey,
                lblCustAction, _txtCustAction,
                sec3, lblPreset, _cmbPreset, bPreset,
                lblColors, lblTop, _pTopBarColor, lblPerso, _pPersoBarColor, lblAccent, _pAccentColor, lblText, _pTextColor,
                _chkStartMax, lblSize, _numWinW, lblX, _numWinH, lblSizeInfo,
                btnSave, btnCancel
            });

            UiScale.ApplyToForm(this);
            LoadSettings();
        }

        // ── Helpers UI ────────────────────────────────────────────────
        private Label MakeSection(string text, int top)
        {
            return new Label
            {
                Text = text, Left = 14, Top = top, Width = 480, Height = 18,
                ForeColor = Color.FromArgb(72, 120, 230),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), AutoSize = false
            };
        }

        private CheckBox MakeCheck(string text, int left, int top)
        {
            return new CheckBox
            {
                Text = text, Left = left, Top = top, Width = 48,
                ForeColor = C_TEXT, BackColor = Color.Transparent
            };
        }

        private ComboBox MakeCombo(int left, int top)
        {
            var cmb = new ComboBox
            {
                Left = left, Top = top, Width = 90, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(24, 30, 52), ForeColor = C_TEXT
            };
            // Touches disponibles
            cmb.Items.Add("(aucune)");
            foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
                cmb.Items.Add(c.ToString());
            for (int i = 1; i <= 12; i++) cmb.Items.Add("F" + i);
            cmb.Items.Add("Tab");
            cmb.Items.Add("Escape");
            cmb.Items.Add("Space");
            cmb.Items.Add("Return");
            cmb.SelectedIndex = 0;
            return cmb;
        }

        private NumericUpDown MakeNum(int left, int top, int min, int max, int def)
        {
            return new NumericUpDown
            {
                Left = left,
                Top = top,
                Width = 78,
                Height = 24,
                Minimum = min,
                Maximum = max,
                Value = def,
                BackColor = C_CARD,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private Panel MakeColorPanel(int left, int top)
        {
            return new Panel
            {
                Left = left,
                Top = top,
                Width = 26,
                Height = 20,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
        }

        private void PickColor(Panel panel)
        {
            using var dlg = new ColorDialog { Color = panel.BackColor, FullOpen = true };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            panel.BackColor = dlg.Color;
            ApplyLivePreview();
        }

        private void ApplyLivePreview()
        {
            if (Owner is MainForm mf)
                mf.ApplyPreviewPalette(
                    _pTopBarColor.BackColor,
                    _pPersoBarColor.BackColor,
                    _pAccentColor.BackColor,
                    _pTextColor.BackColor);
        }

        private void ApplyPreset(string preset)
        {
            switch (preset)
            {
                case "Naruto":
                    _pTopBarColor.BackColor = Color.FromArgb(38, 22, 12);
                    _pPersoBarColor.BackColor = Color.FromArgb(28, 16, 10);
                    _pAccentColor.BackColor = Color.FromArgb(255, 132, 0);
                    _pTextColor.BackColor = Color.FromArgb(247, 232, 208);
                    break;
                case "Dragon Ball":
                    _pTopBarColor.BackColor = Color.FromArgb(8, 24, 62);
                    _pPersoBarColor.BackColor = Color.FromArgb(6, 18, 46);
                    _pAccentColor.BackColor = Color.FromArgb(255, 170, 0);
                    _pTextColor.BackColor = Color.FromArgb(238, 236, 220);
                    break;
                case "Cyberpunk":
                    _pTopBarColor.BackColor = Color.FromArgb(18, 8, 40);
                    _pPersoBarColor.BackColor = Color.FromArgb(10, 6, 26);
                    _pAccentColor.BackColor = Color.FromArgb(0, 236, 255);
                    _pTextColor.BackColor = Color.FromArgb(241, 225, 255);
                    break;
                case "Lave":
                    _pTopBarColor.BackColor = Color.FromArgb(36, 8, 8);
                    _pPersoBarColor.BackColor = Color.FromArgb(24, 6, 6);
                    _pAccentColor.BackColor = Color.FromArgb(255, 74, 38);
                    _pTextColor.BackColor = Color.FromArgb(245, 220, 210);
                    break;
                case "Glacier":
                    _pTopBarColor.BackColor = Color.FromArgb(14, 34, 56);
                    _pPersoBarColor.BackColor = Color.FromArgb(10, 24, 40);
                    _pAccentColor.BackColor = Color.FromArgb(109, 214, 255);
                    _pTextColor.BackColor = Color.FromArgb(232, 246, 255);
                    break;
                case "Soleil":
                    _pTopBarColor.BackColor = Color.FromArgb(52, 32, 8);
                    _pPersoBarColor.BackColor = Color.FromArgb(42, 24, 6);
                    _pAccentColor.BackColor = Color.FromArgb(255, 211, 79);
                    _pTextColor.BackColor = Color.FromArgb(255, 244, 210);
                    break;
                case "Forêt":
                    _pTopBarColor.BackColor = Color.FromArgb(18, 34, 18);
                    _pPersoBarColor.BackColor = Color.FromArgb(12, 24, 12);
                    _pAccentColor.BackColor = Color.FromArgb(106, 187, 62);
                    _pTextColor.BackColor = Color.FromArgb(232, 247, 228);
                    break;
                case "Sakura":
                    _pTopBarColor.BackColor = Color.FromArgb(42, 22, 40);
                    _pPersoBarColor.BackColor = Color.FromArgb(34, 17, 32);
                    _pAccentColor.BackColor = Color.FromArgb(255, 99, 146);
                    _pTextColor.BackColor = Color.FromArgb(250, 225, 236);
                    break;
                case "Émeraude":
                    _pTopBarColor.BackColor = Color.FromArgb(18, 40, 28);
                    _pPersoBarColor.BackColor = Color.FromArgb(12, 30, 20);
                    _pAccentColor.BackColor = Color.FromArgb(46, 204, 113);
                    _pTextColor.BackColor = Color.FromArgb(216, 242, 228);
                    break;
                case "Violet":
                    _pTopBarColor.BackColor = Color.FromArgb(30, 24, 52);
                    _pPersoBarColor.BackColor = Color.FromArgb(22, 17, 40);
                    _pAccentColor.BackColor = Color.FromArgb(178, 114, 255);
                    _pTextColor.BackColor = Color.FromArgb(228, 220, 248);
                    break;
                case "Noir pro":
                    _pTopBarColor.BackColor = Color.FromArgb(20, 20, 20);
                    _pPersoBarColor.BackColor = Color.FromArgb(14, 14, 14);
                    _pAccentColor.BackColor = Color.FromArgb(245, 110, 145);
                    _pTextColor.BackColor = Color.FromArgb(235, 235, 235);
                    break;
                default: // Océan
                    _pTopBarColor.BackColor = Color.FromArgb(18, 22, 40);
                    _pPersoBarColor.BackColor = Color.FromArgb(13, 17, 32);
                    _pAccentColor.BackColor = Color.FromArgb(72, 120, 230);
                    _pTextColor.BackColor = Color.FromArgb(210, 220, 240);
                    break;
            }
        }

        // ── Sauvegarde / Chargement ───────────────────────────────────
        private const string REG = @"HKEY_CURRENT_USER\MultiCompteV3\Hotkeys";

        private void Save(object? sender, EventArgs e)
        {
            Registry.SetValue(REG, "SwAlt",  _chkSwAlt.Checked  ? "1" : "0");
            Registry.SetValue(REG, "SwCtrl", _chkSwCtrl.Checked ? "1" : "0");
            Registry.SetValue(REG, "SwWin",  _chkSwWin.Checked  ? "1" : "0");
            Registry.SetValue(REG, "SwKey",  _cmbSwKey.SelectedItem?.ToString() ?? "(aucune)");

            Registry.SetValue(REG, "CustAlt",    _chkCustAlt.Checked  ? "1" : "0");
            Registry.SetValue(REG, "CustCtrl",   _chkCustCtrl.Checked ? "1" : "0");
            Registry.SetValue(REG, "CustWin",    _chkCustWin.Checked  ? "1" : "0");
            Registry.SetValue(REG, "CustKey",    _cmbCustKey.SelectedItem?.ToString() ?? "(aucune)");
            Registry.SetValue(REG, "CustAction", _txtCustAction.Text.Trim());

            Settings.TopBarColor = _pTopBarColor.BackColor;
            Settings.PersoBarColor = _pPersoBarColor.BackColor;
            Settings.AccentColor = _pAccentColor.BackColor;
            Settings.TextColor = _pTextColor.BackColor;
            Settings.Theme = _cmbPreset.SelectedItem?.ToString() ?? "Océan";
            Settings.StartMaximized = _chkStartMax.Checked;
            Settings.WindowWidth = (int)_numWinW.Value;
            Settings.WindowHeight = (int)_numWinH.Value;

            MessageBox.Show("Options sauvegardées.",
                "Options", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void LoadSettings()
        {
            _chkSwAlt.Checked  = Registry.GetValue(REG, "SwAlt",  "0")?.ToString() == "1";
            _chkSwCtrl.Checked = Registry.GetValue(REG, "SwCtrl", "0")?.ToString() == "1";
            _chkSwWin.Checked  = Registry.GetValue(REG, "SwWin",  "0")?.ToString() == "1";
            SetCombo(_cmbSwKey, Registry.GetValue(REG, "SwKey", "(aucune)")?.ToString());

            _chkCustAlt.Checked  = Registry.GetValue(REG, "CustAlt",  "0")?.ToString() == "1";
            _chkCustCtrl.Checked = Registry.GetValue(REG, "CustCtrl", "0")?.ToString() == "1";
            _chkCustWin.Checked  = Registry.GetValue(REG, "CustWin",  "0")?.ToString() == "1";
            SetCombo(_cmbCustKey, Registry.GetValue(REG, "CustKey", "(aucune)")?.ToString());
            _txtCustAction.Text = Registry.GetValue(REG, "CustAction", "")?.ToString() ?? "";

            _pTopBarColor.BackColor = Settings.TopBarColor;
            _pPersoBarColor.BackColor = Settings.PersoBarColor;
            _pAccentColor.BackColor = Settings.AccentColor;
            _pTextColor.BackColor = Settings.TextColor;
            SetCombo(_cmbPreset, Settings.Theme);
            _chkStartMax.Checked = Settings.StartMaximized;
            _numWinW.Value = Math.Clamp(Settings.WindowWidth, (int)_numWinW.Minimum, (int)_numWinW.Maximum);
            _numWinH.Value = Math.Clamp(Settings.WindowHeight, (int)_numWinH.Minimum, (int)_numWinH.Maximum);

            ApplyLivePreview();
        }

        private static void SetCombo(ComboBox c, string? val)
        {
            if (val == null) { c.SelectedIndex = 0; return; }
            int idx = c.Items.IndexOf(val);
            c.SelectedIndex = idx >= 0 ? idx : 0;
        }

        // ── Lecture statique des paramètres sauvegardés ───────────────
        public static HotkeyConfig LoadSwitch()
        {
            return new HotkeyConfig
            {
                Alt  = Registry.GetValue(REG, "SwAlt",  "0")?.ToString() == "1",
                Ctrl = Registry.GetValue(REG, "SwCtrl", "0")?.ToString() == "1",
                Win  = Registry.GetValue(REG, "SwWin",  "0")?.ToString() == "1",
                Key  = Registry.GetValue(REG, "SwKey",  "(aucune)")?.ToString() ?? "(aucune)"
            };
        }

        public static HotkeyConfig LoadCustom()
        {
            return new HotkeyConfig
            {
                Alt    = Registry.GetValue(REG, "CustAlt",  "0")?.ToString() == "1",
                Ctrl   = Registry.GetValue(REG, "CustCtrl", "0")?.ToString() == "1",
                Win    = Registry.GetValue(REG, "CustWin",  "0")?.ToString() == "1",
                Key    = Registry.GetValue(REG, "CustKey",  "(aucune)")?.ToString() ?? "(aucune)",
                Action = Registry.GetValue(REG, "CustAction", "")?.ToString() ?? ""
            };
        }
    }

    internal class HotkeyConfig
    {
        public bool   Alt, Ctrl, Win;
        public string Key    = "(aucune)";
        public string Action = "";

        public uint Modifiers =>
            (Alt  ? 0x0001u : 0) |
            (Ctrl ? 0x0002u : 0) |
            (Win  ? 0x0008u : 0);

        public uint Vk => KeyNameToVk(Key);

        public bool IsValid => Key != "(aucune)" && Vk != 0;

        private static uint KeyNameToVk(string name)
        {
            if (name.Length == 1 && char.IsUpper(name[0]))
                return (uint)name[0];
            if (name.StartsWith("F") && int.TryParse(name[1..], out int fn) && fn >= 1 && fn <= 12)
                return (uint)(0x6F + fn); // F1=0x70
            return name switch
            {
                "Tab"    => 0x09,
                "Escape" => 0x1B,
                "Space"  => 0x20,
                "Return" => 0x0D,
                _ => 0
            };
        }
    }
}

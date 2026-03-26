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

        public OptionsForm()
        {
            Text            = "Options — Raccourcis";
            Size            = new Size(520, 370);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            BackColor       = C_BG;
            ForeColor       = C_TEXT;
            Font            = new Font("Segoe UI", 9f);

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

            var btnSave = new Button
            {
                Text = "💾  Sauvegarder", Left = 322, Top = 300, Width = 160, Height = 30,
                BackColor = C_ACC, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += Save;

            var btnCancel = new Button
            {
                Text = "Annuler", Left = 234, Top = 300, Width = 82, Height = 30,
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
                btnSave, btnCancel
            });

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

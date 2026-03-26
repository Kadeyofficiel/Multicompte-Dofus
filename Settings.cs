using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MultiCompte
{
    internal static class Settings
    {
        private const string KEY = @"HKEY_CURRENT_USER\MultiCompteV3\Settings";

        public static string Get(string name, string def = "") =>
            Registry.GetValue(KEY, name, def)?.ToString() ?? def;

        public static void Set(string name, string value) =>
            Registry.SetValue(KEY, name, value);

        private static int GetInt(string name, int def)
        {
            string raw = Get(name, def.ToString());
            return int.TryParse(raw, out int v) ? v : def;
        }

        // ── Mémorisation sélection ────────────────────────────────────
        public static bool RememberEnabled
        {
            get => Get("rememberEnabled") == "1";
            set => Set("rememberEnabled", value ? "1" : "0");
        }

        public static HashSet<string> RememberedNames
        {
            get
            {
                string raw = Get("rememberedNames");
                if (string.IsNullOrWhiteSpace(raw))
                    return new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                return new HashSet<string>(
                    raw.Split('|', System.StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => s.Trim()),
                    System.StringComparer.OrdinalIgnoreCase);
            }
            set => Set("rememberedNames", string.Join("|", value.Distinct()));
        }

        // ── Chemin Dofus.exe ─────────────────────────────────────────
        public static string DofusPath
        {
            get => Get("dofusPath");
            set => Set("dofusPath", value);
        }

        // ── Apparence / Fenêtre ───────────────────────────────────────
        public static string Theme
        {
            get => Get("theme", "Bleu");
            set => Set("theme", value);
        }

        public static bool StartMaximized
        {
            get => Get("startMaximized", "1") == "1";
            set => Set("startMaximized", value ? "1" : "0");
        }

        public static int WindowWidth
        {
            get => GetInt("windowWidth", 1366);
            set => Set("windowWidth", value.ToString());
        }

        public static int WindowHeight
        {
            get => GetInt("windowHeight", 800);
            set => Set("windowHeight", value.ToString());
        }

        public static Color TopBarColor
        {
            get => GetColor("topBarColor", Color.FromArgb(18, 22, 40));
            set => Set("topBarColor", ColorToString(value));
        }

        public static Color PersoBarColor
        {
            get => GetColor("persoBarColor", Color.FromArgb(13, 17, 32));
            set => Set("persoBarColor", ColorToString(value));
        }

        public static Color AccentColor
        {
            get => GetColor("accentColor", Color.FromArgb(233, 69, 96));
            set => Set("accentColor", ColorToString(value));
        }

        public static Color TextColor
        {
            get => GetColor("textColor", Color.FromArgb(210, 220, 240));
            set => Set("textColor", ColorToString(value));
        }

        private static string ColorToString(Color c) => $"{c.R},{c.G},{c.B}";

        private static Color GetColor(string name, Color def)
        {
            string raw = Get(name, ColorToString(def));
            var p = raw.Split(',');
            if (p.Length == 3 &&
                int.TryParse(p[0], out int r) &&
                int.TryParse(p[1], out int g) &&
                int.TryParse(p[2], out int b))
            {
                return Color.FromArgb(
                    Math.Clamp(r, 0, 255),
                    Math.Clamp(g, 0, 255),
                    Math.Clamp(b, 0, 255));
            }
            return def;
        }
    }
}

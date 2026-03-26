using Microsoft.Win32;
using System.Collections.Generic;
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
    }
}

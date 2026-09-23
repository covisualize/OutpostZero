using System;
using System.Collections.Generic;

namespace OutpostZero.Shell
{
    public enum TextScript
    {
        Latin,
        Cyrillic,
        Cjk
    }

    /// <summary>
    /// Which installed operating-system fonts carry each script, so a translation renders without shipping font files.
    /// Latin languages keep the default UI Toolkit font; other scripts get their own font first and the rest as fallbacks.
    /// </summary>
    public static class FontChain
    {
        public static readonly TextScript[] Order = { TextScript.Latin, TextScript.Cyrillic, TextScript.Cjk };

        private static readonly string[] cyrillicCodes = { "ru", "uk", "be", "bg", "sr", "mk", "kk", "ky", "mn", "tg" };

        private static readonly string[] latin = { "Noto Sans", "Segoe UI", "Arial", "Helvetica Neue", "Helvetica", "DejaVu Sans", "Liberation Sans" };
        private static readonly string[] cyrillic = { "Noto Sans", "Segoe UI", "Arial", "Helvetica Neue", "DejaVu Sans", "Liberation Sans" };
        private static readonly string[] chinese = { "Noto Sans CJK SC", "Noto Sans SC", "Microsoft YaHei", "PingFang SC", "Source Han Sans SC", "WenQuanYi Micro Hei", "SimHei" };
        private static readonly string[] japanese = { "Noto Sans CJK JP", "Noto Sans JP", "Yu Gothic", "Meiryo", "Hiragino Sans", "Source Han Sans JP", "MS Gothic" };
        private static readonly string[] korean = { "Noto Sans CJK KR", "Noto Sans KR", "Malgun Gothic", "Apple SD Gothic Neo", "Source Han Sans KR", "NanumGothic" };

        public static string Base(string language)
        {
            if (string.IsNullOrEmpty(language)) return "en";
            string code = language.ToLowerInvariant();
            int cut = code.IndexOfAny(new[] { '-', '_' });
            return cut > 0 ? code.Substring(0, cut) : code;
        }

        public static TextScript ScriptOf(string language)
        {
            string code = Base(language);
            if (code == "zh" || code == "ja" || code == "ko") return TextScript.Cjk;
            return Array.IndexOf(cyrillicCodes, code) >= 0 ? TextScript.Cyrillic : TextScript.Latin;
        }

        /// <summary>Characters a font must hold before it is trusted for the script.</summary>
        public static string Probe(TextScript script, string language)
        {
            switch (script)
            {
                case TextScript.Cyrillic: return "ЖжЯяЁё";
                case TextScript.Cjk:
                    string code = Base(language);
                    if (code == "ja") return "字あア";
                    if (code == "ko") return "한글";
                    return "字汉";
                default: return "AaÉéÑñ";
            }
        }

        /// <summary>Preferred font families for a script, best first. CJK follows the language so Han glyphs match it.</summary>
        public static string[] Families(TextScript script, string language)
        {
            switch (script)
            {
                case TextScript.Cyrillic: return cyrillic;
                case TextScript.Cjk:
                    string code = Base(language);
                    if (code == "ja") return japanese;
                    if (code == "ko") return korean;
                    return chinese;
                default: return latin;
            }
        }

        public static string Pick(IEnumerable<string> installed, string[] preferred)
        {
            if (installed == null || preferred == null) return "";
            var have = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);
            foreach (var family in preferred)
            {
                if (have.Contains(family)) return family;
            }
            return "";
        }

        /// <summary>Whether the panels need a custom font at all; Latin text stays on the default font.</summary>
        public static bool Needed(string language) => ScriptOf(language) != TextScript.Latin;

        /// <summary>
        /// The scripts to load for a language: its own first, then the others in Latin, Cyrillic, CJK order.
        /// </summary>
        public static TextScript[] Scripts(string language)
        {
            var own = ScriptOf(language);
            var list = new List<TextScript> { own };
            foreach (var script in Order)
            {
                if (script != own) list.Add(script);
            }
            return list.ToArray();
        }
    }
}

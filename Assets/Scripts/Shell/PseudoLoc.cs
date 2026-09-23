using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// A test language for dev builds. Every table string comes back accented, a third longer and
    /// bracketed, so text that skips the string table shows up plain and a line that overflows its
    /// box shows up clipped. {key:Action} tokens pass through so key prompts still fill.
    /// </summary>
    public static class PseudoLoc
    {
        public const string Code = "qps";
        public const char Open = '[';
        public const char Close = ']';
        public const float Growth = 0.34f;

        private const string Plain = "aeiouyncsAEIOUYNCS";
        private const string Accented = "áéíóúýñçšÁÉÍÓÚÝÑÇŠ";

        public static string Wrap(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var builder = new StringBuilder(text.Length * 2);
            builder.Append(Open);
            int letters = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{')
                {
                    int end = text.IndexOf('}', i);
                    if (end > i)
                    {
                        builder.Append(text, i, end - i + 1);
                        i = end;
                        continue;
                    }
                }
                int at = Plain.IndexOf(c);
                builder.Append(at >= 0 ? Accented[at] : c);
                if (char.IsLetter(c)) letters++;
            }
            int pad = (int)System.Math.Ceiling(letters * Growth);
            if (pad > 0) builder.Append(' ').Append('~', pad);
            builder.Append(Close);
            return builder.ToString();
        }

        public static bool Wrapped(string text)
        {
            return !string.IsNullOrEmpty(text) && text[0] == Open && text[text.Length - 1] == Close;
        }

        /// <summary>The languages the settings button cycles: built-in, then translator packs, then the dev test language.</summary>
        public static string[] Languages(bool dev)
        {
            var codes = new System.Collections.Generic.List<string> { "en", "es" };
            codes.AddRange(Loc.Packs);
            if (dev) codes.Add(Code);
            return codes.ToArray();
        }

        public static string Next(string current, bool dev)
        {
            var codes = Languages(dev);
            for (int i = 0; i < codes.Length; i++)
                if (codes[i] == current) return codes[(i + 1) % codes.Length];
            return codes[0];
        }

        public static string Keep(string saved, bool dev)
        {
            var codes = Languages(dev);
            for (int i = 0; i < codes.Length; i++)
                if (codes[i] == saved) return saved;
            return codes[0];
        }

        public static string Label(string code)
        {
            if (code == "es") return "Idioma: ES";
            if (code == Code) return Wrap("Language: pseudo");
            if (Loc.IsPack(code)) return "Language: " + Loc.PackName(code);
            return "Language: EN";
        }
    }
}

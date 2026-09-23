using System;
using System.Text;
using OutpostZero.Player;

namespace OutpostZero.Shell
{
    /// <summary>
    /// String-table text names keys as {key:Action}. Filling reads the live bindings and the last
    /// device used, so a rebound key or a picked-up pad shows up in every hint, lesson and prompt.
    /// </summary>
    public static class KeyPrompt
    {
        public const string Open = "{key:";

        public static string Token(ControlBindings.Action action) => Open + action + "}";

        public static string Fill(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf(Open, StringComparison.Ordinal) < 0) return text;
            var builder = new StringBuilder(text.Length);
            int at = 0;
            while (at < text.Length)
            {
                int start = text.IndexOf(Open, at, StringComparison.Ordinal);
                if (start < 0) break;
                int end = text.IndexOf('}', start);
                if (end < 0) break;
                builder.Append(text, at, start - at);
                string name = text.Substring(start + Open.Length, end - start - Open.Length);
                if (name.Length > 0 && char.IsLetter(name[0]) && Enum.TryParse(name, false, out ControlBindings.Action action))
                    builder.Append(InputGlyphs.Label(action));
                else
                    builder.Append(text, start, end - start + 1);
                at = end + 1;
            }
            builder.Append(text, at, text.Length - at);
            return builder.ToString();
        }

        public static string Interact(string prompt)
        {
            return "[" + InputGlyphs.Label(ControlBindings.Action.Interact) + "] " + prompt;
        }
    }

    /// <summary>
    /// What the key list says after a rebind the game refused.
    /// </summary>
    public static class BindNote
    {
        public static string For(ControlBindings.RebindResult result, UnityEngine.InputSystem.Key key, int holder, string language)
        {
            string name = ControlBindings.Name(key);
            if (result == ControlBindings.RebindResult.Taken && holder >= 0)
                return string.Format(Word("bind.taken", language), name, ((ControlBindings.Action)holder).ToString());
            if (result == ControlBindings.RebindResult.Reserved)
                return string.Format(Word("bind.reserved", language), name);
            return "";
        }

        private static string Word(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Loc.T(key) : Loc.T(key, language);
        }
    }
}

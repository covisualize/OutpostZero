using System.Text;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A craft row names its materials in the active language.
    /// English keeps the same lowercase words the bench already used.
    /// </summary>
    public static class CraftSay
    {
        public static string Line(string label, int scrap, int cloth, int chemicals, int tape, string language)
        {
            if (scrap < 0) scrap = 0;
            if (cloth < 0) cloth = 0;
            if (chemicals < 0) chemicals = 0;
            if (tape < 0) tape = 0;
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrEmpty(label) ? Word("bill.craft", language) : label);
            if (scrap > 0) builder.Append("   ").Append(Word("bill.scrap", language)).Append(' ').Append(scrap);
            if (cloth > 0) builder.Append("   ").Append(Word("bill.cloth", language)).Append(' ').Append(cloth);
            if (chemicals > 0) builder.Append("   ").Append(Word("bill.chem", language)).Append(' ').Append(chemicals);
            if (tape > 0) builder.Append("   ").Append(Word("bill.tape", language)).Append(' ').Append(tape);
            return builder.ToString();
        }

        public static string Fitted(string id, string label, string language)
        {
            string key = "recipe." + (id ?? "");
            string name = Word(key, language);
            if (name == key || string.IsNullOrEmpty(name)) name = label ?? "";
            return name + " " + Word("craft.fitted", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }
    }
}

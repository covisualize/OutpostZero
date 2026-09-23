using System.Text;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// What a module costs to mark out: scrap plus the camp supplies some modules also need. A cot wants cloth
    /// for its canvas, a generator tape and chemicals. Demolishing gives back half of each, rounded down.
    /// </summary>
    public struct ModuleBill
    {
        public int Scrap;
        public int Cloth;
        public int Chemicals;
        public int Tape;

        public ModuleBill(int scrap, int cloth, int chemicals, int tape)
        {
            Scrap = scrap < 0 ? 0 : scrap;
            Cloth = cloth < 0 ? 0 : cloth;
            Chemicals = chemicals < 0 ? 0 : chemicals;
            Tape = tape < 0 ? 0 : tape;
        }

        public bool ScrapOnly => Cloth == 0 && Chemicals == 0 && Tape == 0;

        public bool Affords(int scrap, int cloth, int chemicals, int tape)
        {
            return scrap >= Scrap && cloth >= Cloth && chemicals >= Chemicals && tape >= Tape;
        }

        public ModuleBill Half()
        {
            return new ModuleBill(ScrapRefund.Half(Scrap), ScrapRefund.Half(Cloth), ScrapRefund.Half(Chemicals), ScrapRefund.Half(Tape));
        }

        /// <summary>The bill as "8 scrap, 1 cloth": scrap always, supplies only when owed.</summary>
        public string Parts(string language)
        {
            var text = new StringBuilder();
            text.Append(Scrap).Append(' ').Append(Word("bill.scrap", language));
            Part(text, Cloth, "bill.cloth", language);
            Part(text, Chemicals, "bill.chem", language);
            Part(text, Tape, "bill.tape", language);
            return text.ToString();
        }

        /// <summary>A build button: the module name and its scrap, then any supplies, as "Cot 8 +1 cloth".</summary>
        public string Button(string label)
        {
            var text = new StringBuilder(label ?? "");
            text.Append(' ').Append(Scrap);
            Extra(text, Cloth, "bill.cloth");
            Extra(text, Chemicals, "bill.chem");
            Extra(text, Tape, "bill.tape");
            return text.ToString();
        }

        private static void Part(StringBuilder text, int amount, string key, string language)
        {
            if (amount <= 0) return;
            text.Append(", ").Append(amount).Append(' ').Append(Word(key, language));
        }

        private static void Extra(StringBuilder text, int amount, string key)
        {
            if (amount <= 0) return;
            text.Append(" +").Append(amount).Append(' ').Append(Loc.T(key));
        }

        private static string Word(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Loc.T(key) : Loc.T(key, language);
        }
    }
}

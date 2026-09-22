using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The stall, the station prompt, and a blocked recipe follow the language.
    /// The English craft block stays the same sentence the bench already used.
    /// </summary>
    public static class StallVoice
    {
        public static string Name(string id, string language)
        {
            if (id == "militia") return Word("stall.militia", language);
            if (id == "clinic") return Word("stall.clinic", language);
            if (id == "farmers") return Word("stall.farmers", language);
            return Word("stall.caravan", language);
        }

        public static string Prompt(StationKind kind, string language)
        {
            if (kind == StationKind.Workbench) return Word("stall.bench", language);
            if (kind == StationKind.Campfire) return Word("stall.fire", language);
            if (kind == StationKind.MedicalCot) return Word("stall.cot", language);
            if (kind == StationKind.Water) return Word("stall.water", language);
            if (kind == StationKind.Generator) return Word("stall.gen", language);
            return Word("stall.trade", language);
        }

        public static string Block(string english, string language)
        {
            if (english == "Need a workbench") return Word("gate.bench", language);
            if (english == "Need a medical cot") return Word("gate.cot", language);
            if (english == "Need a station") return Word("gate.station", language);
            if (english == "Need a medic on duty") return Word("gate.medic", language);
            return english ?? "";
        }

        public static string Quest(string id, bool done, string language)
        {
            if (id == "clinic") return Word(done ? "quest.clinic_done" : "quest.clinic", language);
            if (id == "farmers") return Word(done ? "quest.farmers_done" : "quest.farmers", language);
            if (id == "militia") return Word("quest.militia", language);
            return Word(done ? "quest.caravan_done" : "quest.caravan", language);
        }

        public static string Buy(string label, int price, string language)
        {
            if (price < 0) price = 0;
            if (string.IsNullOrEmpty(label)) label = "";
            return Word("stall.buy", language) + " " + label + " (" + price + ")";
        }

        public static string Refuse(string faction, string language)
        {
            if (string.IsNullOrEmpty(faction)) faction = "";
            return faction + " " + Word("stall.refuse", language);
        }

        public static string Wait(string language) => Word("stall.wait", language);
        public static string Shake(string language) => Word("stall.shake", language);
        public static string Full(string language) => Word("stall.pack", language);
        public static string NoBandage(string language) => Word("stall.nobandage", language);
        public static string Blueprint(string language) => Word("stall.blueprint", language);
        public static string Nest(string language) => Word("stall.nest", language);
        public static string Through(string language) => Word("stall.through", language);

        public static string Bartered(int scrap, string language)
        {
            if (scrap < 0) scrap = 0;
            return Word("stall.bartered", language) + " " + scrap + " " + Word("stall.barterscrap", language);
        }

        public static string Deal(string id, int stock, string language)
        {
            string line = Name(id, language) + " " + Word("stall.sealed", language);
            if (stock > 0) line += "  " + Word("camp.rounds", language) + " +" + stock;
            return line;
        }

        public static string Arrival(string id, string language)
        {
            return Name(id, language) + " " + Word("stall.gate", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }
    }
}

namespace OutpostZero.Colony
{
    /// <summary>
    /// The one colonist who goes out beside the leader. They pack a day's food and water from camp storage at the
    /// gate, cover the leader with shots set by Guard, carry extra scrap home by Scavenge, and bring every bite
    /// back to the roster. A survivor carrying a wound past the first stays in camp.
    /// </summary>
    public static class CompanionKit
    {
        public const string Task = "Companion";
        public const int WoundLimit = 1;
        public const float Range = 9f;
        public const float Pace = 4.6f;
        public const float HungryMorale = 8f;

        public static bool Fit(bool alive, bool leader, int injury)
        {
            return alive && !leader && injury <= WoundLimit;
        }

        /// <summary>One food and one water each, taken only when storage holds them.</summary>
        public static bool Pack(int food, int water, out int takeFood, out int takeWater)
        {
            takeFood = food > 0 ? 1 : 0;
            takeWater = water > 0 ? 1 : 0;
            return takeFood > 0 && takeWater > 0;
        }

        public static float Damage(int combat)
        {
            if (combat < 0) combat = 0;
            return 10f + 3f * combat;
        }

        public static float Cadence(int combat)
        {
            if (combat < 0) combat = 0;
            float gap = 2.6f - 0.12f * combat;
            return gap < 1.2f ? 1.2f : gap;
        }

        public static bool Fires(float last, float now, int combat, float nearest)
        {
            if (nearest < 0f || nearest > Range) return false;
            if (last <= 0f) return true;
            if (now < last) return false;
            return OutpostZero.Core.Tick.Past(now, last, Cadence(combat));
        }

        /// <summary>Extra scrap a companion who came home carries into storage.</summary>
        public static int Haul(int scavenge, bool cameHome)
        {
            if (!cameHome || scavenge <= 0) return 0;
            return 1 + scavenge / 2;
        }

        public static int Wounds(int injury, int bites, bool fell)
        {
            int held = injury + (bites < 0 ? 0 : bites) + (fell ? 1 : 0);
            return held > 3 ? 3 : held;
        }

        public static string Line(string name, bool fell, int haul, string language)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (fell) return name + " " + Word("companion.fell", language);
            string back = name + " " + Word("companion.home", language);
            return haul > 0 ? back + " +" + haul + " " + Word("result.scrap", language) : back;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}

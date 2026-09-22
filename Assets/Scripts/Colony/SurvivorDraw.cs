namespace OutpostZero.Colony
{
    /// <summary>
    /// A new outpost draws four hands from a seed.
    /// The same seed always opens the same camp. A different seed opens a different one.
    /// </summary>
    public static class SurvivorDraw
    {
        public struct Draft
        {
            public string Id;
            public string Name;
            public string Trait;
            public bool Leader;
            public string Bond;
            public int Combat;
            public int Medicine;
            public int Engineering;
            public int Cooking;
            public int Scavenge;
        }

        private static readonly string[] First =
        {
            "Mara", "Jonas", "Priya", "Ellis", "Imani", "Dell", "Rook", "Sela",
            "Omar", "Nina", "Kai", "Vera", "Theo", "Lana", "Cruz", "Ash"
        };

        private static readonly string[] Last =
        {
            "Quill", "Reed", "Sen", "Ward", "Cole", "Orth", "Vance", "Park",
            "Hale", "Moss", "Cruz", "Ng", "Bell", "Diaz", "Cho", "Frost"
        };

        private static readonly string[] Traits =
        {
            "Steady Hands", "Light Sleeper", "Field Medic", "Scrounger", "Watchful", "Volatile", "Glutton",
            "Engineer", "Cook", "Sharpshooter", "Brave", "Cowardly"
        };

        public static Draft[] Open(int seed)
        {
            if (seed == 0) seed = 1701;
            var drafts = new Draft[4];
            var usedFirst = new bool[First.Length];
            var usedTrait = new bool[Traits.Length];
            for (int i = 0; i < 4; i++)
            {
                int firstIndex = Pick(seed, i * 3, First.Length, usedFirst);
                int lastIndex = Mix(seed, i * 5 + 1) % Last.Length;
                int traitIndex = Pick(seed, i * 7 + 2, Traits.Length, usedTrait);
                string first = First[firstIndex];
                string last = Last[lastIndex];
                string trait = Traits[traitIndex];
                drafts[i] = new Draft
                {
                    Id = first.ToLowerInvariant() + "_" + (Mix(seed, i + 11) % 90 + 10),
                    Name = first + " " + last,
                    Trait = trait,
                    Leader = i == 0,
                    Bond = "",
                    Combat = Skill(trait, "combat"),
                    Medicine = Skill(trait, "medicine"),
                    Engineering = Skill(trait, "engineering"),
                    Cooking = Skill(trait, "cooking"),
                    Scavenge = Skill(trait, "scavenge")
                };
            }
            Bond(ref drafts[0], drafts[1].Name, seed, 0);
            Bond(ref drafts[1], drafts[0].Name, seed, 1);
            Bond(ref drafts[2], drafts[3].Name, seed, 2);
            Bond(ref drafts[3], drafts[2].Name, seed, 3);
            return drafts;
        }

        public static string Signature(Draft[] drafts)
        {
            if (drafts == null || drafts.Length == 0) return "";
            string text = "";
            for (int i = 0; i < drafts.Length; i++)
            {
                if (i > 0) text += "|";
                text += drafts[i].Id + ":" + drafts[i].Trait;
            }
            return text;
        }

        private static void Bond(ref Draft person, string otherName, int seed, int slot)
        {
            string first = FirstOf(otherName);
            person.Bond = person.Leader || Mix(seed, slot + 40) % 2 == 0
                ? "Close to " + first
                : "Trusts " + first;
        }

        private static string FirstOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int space = name.IndexOf(' ');
            return space < 0 ? name : name.Substring(0, space);
        }

        private static int Skill(string trait, string kind)
        {
            if (kind == "combat" && trait == "Sharpshooter") return 4;
            if (kind == "combat" && (trait == "Steady Hands" || trait == "Watchful")) return 3;
            if (kind == "combat" && trait == "Brave") return 2;
            if (kind == "medicine" && trait == "Field Medic") return 4;
            if (kind == "scavenge" && trait == "Scrounger") return 3;
            if (kind == "engineering" && trait == "Engineer") return 4;
            if (kind == "engineering" && trait == "Steady Hands") return 2;
            if (kind == "cooking" && trait == "Cook") return 4;
            if (kind == "cooking" && trait == "Light Sleeper") return 1;
            return 0;
        }

        private static int Pick(int seed, int salt, int length, bool[] used)
        {
            int start = Mix(seed, salt) % length;
            for (int i = 0; i < length; i++)
            {
                int index = (start + i) % length;
                if (used[index]) continue;
                used[index] = true;
                return index;
            }
            return start;
        }

        private static int Mix(int seed, int salt)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= (uint)(salt * 747796405 + 2891336453);
                value = (value ^ (value >> 16)) * 2246822519u;
                value = (value ^ (value >> 13)) * 3266489917u;
                return (int)(value ^ (value >> 16)) & 0x7fffffff;
            }
        }
    }
}

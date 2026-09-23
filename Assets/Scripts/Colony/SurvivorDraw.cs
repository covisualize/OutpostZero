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
            public string Aside;
            public string Mark;
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

        public static Draft[] Open(int seed)
        {
            if (seed == 0) seed = 1701;
            string[] traits = TraitTable.Ids();
            var drafts = new Draft[4];
            var usedFirst = new bool[First.Length];
            var usedTrait = new bool[traits.Length];
            for (int i = 0; i < 4; i++)
            {
                int firstIndex = Pick(seed, i * 3, First.Length, usedFirst);
                int lastIndex = Mix(seed, i * 5 + 1) % Last.Length;
                int traitIndex = Pick(seed, i * 7 + 2, traits.Length, usedTrait);
                string first = First[firstIndex];
                string last = Last[lastIndex];
                string trait = traits[traitIndex];
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
            for (int i = 0; i < 4; i++)
            {
                int asideIndex = Second(seed, i * 7 + 90, usedTrait, drafts[i].Trait, traits);
                string aside = asideIndex < 0 ? "" : traits[asideIndex];
                drafts[i].Aside = aside;
                drafts[i].Combat = System.Math.Max(drafts[i].Combat, Skill(aside, "combat"));
                drafts[i].Medicine = System.Math.Max(drafts[i].Medicine, Skill(aside, "medicine"));
                drafts[i].Engineering = System.Math.Max(drafts[i].Engineering, Skill(aside, "engineering"));
                drafts[i].Cooking = System.Math.Max(drafts[i].Cooking, Skill(aside, "cooking"));
                drafts[i].Scavenge = System.Math.Max(drafts[i].Scavenge, Skill(aside, "scavenge"));
            }
            for (int i = 0; i < 4; i++)
            {
                int markIndex = Third(seed, i * 7 + 140, usedTrait, drafts[i].Trait, drafts[i].Aside, traits);
                drafts[i].Mark = markIndex < 0 ? "" : traits[markIndex];
            }
            return drafts;
        }

        public static bool Clashes(string a, string b)
        {
            return TraitTable.Clashes(a, b);
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
            return TraitTable.Skill(trait, kind);
        }

        private static int Second(int seed, int salt, bool[] used, string first, string[] traits)
        {
            int start = Mix(seed, salt) % traits.Length;
            for (int i = 0; i < traits.Length; i++)
            {
                int index = (start + i) % traits.Length;
                if (used[index]) continue;
                if (Clashes(first, traits[index])) continue;
                used[index] = true;
                return index;
            }
            return -1;
        }

        private static int Third(int seed, int salt, bool[] used, string first, string second, string[] traits)
        {
            int start = Mix(seed, salt) % traits.Length;
            for (int i = 0; i < traits.Length; i++)
            {
                int index = (start + i) % traits.Length;
                if (used[index]) continue;
                string name = traits[index];
                if (Clashes(first, name) || Clashes(second, name)) continue;
                used[index] = true;
                return index;
            }
            return -1;
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

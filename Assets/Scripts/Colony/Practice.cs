using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A colonist gets better at the work they keep doing. Every paid shift is one point of experience.
    /// Below skill 4 one point raises the skill, so the fourth shift is still the first one that pays an extra point.
    /// From 4 up a level costs its skill less two points: 2 to reach 5, and 7 to reach the ceiling of 10.
    /// An empty save is all zeros.
    /// </summary>
    public static class Practice
    {
        public const int Cap = 10;
        public const int BonusAt = 4;

        public static int Need(int skill)
        {
            if (skill < BonusAt) return 1;
            return skill - 2;
        }

        public static void Train(ref int skill, ref int xp)
        {
            if (skill < 0) skill = 0;
            if (xp < 0) xp = 0;
            if (skill >= Cap)
            {
                skill = Cap;
                xp = 0;
                return;
            }
            xp++;
            if (xp < Need(skill)) return;
            skill++;
            xp = 0;
        }

        public static int Shifts(int from, int to)
        {
            if (from < 0) from = 0;
            if (to > Cap) to = Cap;
            int total = 0;
            for (int level = from; level < to; level++) total += Need(level);
            return total;
        }

        public static string PackXp(int combat, int medicine, int engineering, int cooking, int scavenge, int leadership)
        {
            return Xp(combat) + "," + Xp(medicine) + "," + Xp(engineering) + "," + Xp(cooking) + "," + Xp(scavenge) + "," + Xp(leadership);
        }

        public static int ReadXp(string packed, int index)
        {
            if (string.IsNullOrEmpty(packed) || index < 0) return 0;
            string[] parts = packed.Split(',');
            if (index >= parts.Length) return 0;
            int value = 0;
            string token = parts[index];
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c < '0' || c > '9') continue;
                value = value * 10 + (c - '0');
                if (value > Need(Cap)) return Need(Cap);
            }
            return value;
        }

        private static int Xp(int xp)
        {
            return xp < 0 ? 0 : xp;
        }

        public static int Bonus(int skill)
        {
            if (skill < BonusAt) return 0;
            return 1;
        }

        public static string Pack(int combat, int medicine, int engineering, int cooking, int scavenge)
        {
            return Clamp(combat) + "," + Clamp(medicine) + "," + Clamp(engineering) + "," + Clamp(cooking) + "," + Clamp(scavenge);
        }

        public static void Unpack(string packed, out int combat, out int medicine, out int engineering, out int cooking, out int scavenge)
        {
            combat = 0;
            medicine = 0;
            engineering = 0;
            cooking = 0;
            scavenge = 0;
            if (string.IsNullOrEmpty(packed)) return;
            string[] parts = packed.Split(',');
            if (parts.Length > 0) combat = Read(parts[0]);
            if (parts.Length > 1) medicine = Read(parts[1]);
            if (parts.Length > 2) engineering = Read(parts[2]);
            if (parts.Length > 3) cooking = Read(parts[3]);
            if (parts.Length > 4) scavenge = Read(parts[4]);
        }

        public static string Line(int combat, int medicine, int engineering, int cooking, int scavenge, string language)
        {
            string text = "";
            Append(ref text, "Guard", combat, language);
            Append(ref text, "Medic", medicine, language);
            Append(ref text, "Build", engineering, language);
            Append(ref text, "Cook", cooking, language);
            Append(ref text, "Scavenge", scavenge, language);
            return text;
        }

        private static void Append(ref string text, string task, int skill, string language)
        {
            if (skill <= 0) return;
            if (text.Length > 0) text += "  ";
            text += Loc.Task(task, language) + " " + skill;
        }

        private static int Clamp(int skill)
        {
            if (skill < 0) return 0;
            if (skill > Cap) return Cap;
            return skill;
        }

        private static int Read(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            int value = 0;
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                if (c < '0' || c > '9') continue;
                value = value * 10 + (c - '0');
            }
            return Clamp(value);
        }
    }
}

using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A colonist gets better at the work they keep doing.
    /// The fourth shift is the first one that pays an extra point. Eight is the ceiling.
    /// An empty save is all zeros.
    /// </summary>
    public static class Practice
    {
        public const int Cap = 8;
        public const int BonusAt = 4;

        public static int Gain(int skill)
        {
            if (skill < 0) skill = 0;
            if (skill >= Cap) return Cap;
            return skill + 1;
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

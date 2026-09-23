using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Each hand has an age and a past. A young night restores 10 morale, a usual night 8,
    /// and a night past 52 restores 5. An old save has no age and keeps the old 8.
    /// </summary>
    public static class LifeLine
    {
        public const int Young = 28;
        public const int Old = 52;

        public static int YearsOf(string id)
        {
            return 22 + (Hash(id) % 40);
        }

        public static string Past(string id)
        {
            string[] keys =
            {
                "past.nurse", "past.driver", "past.teacher", "past.mechanic", "past.clerk", "past.soldier"
            };
            return keys[Hash(id) % keys.Length];
        }

        public static int Rest(int age)
        {
            if (age <= 0) return 8;
            if (age >= Old) return 5;
            if (age <= Young) return 10;
            return 8;
        }

        public static string Line(int age, string past, string language)
        {
            if (age <= 0) return "";
            if (string.IsNullOrEmpty(past)) return age.ToString();
            string story = language == null ? Loc.T(past) : Loc.T(past, language);
            if (story == past || story.Length == 0) return age.ToString();
            return age + "  " + story;
        }

        private static int Hash(string id)
        {
            if (string.IsNullOrEmpty(id)) return 34;
            int hash = 17;
            for (int i = 0; i < id.Length; i++) hash = unchecked(hash * 31 + id[i]);
            if (hash < 0) hash = -hash;
            return hash;
        }
    }
}

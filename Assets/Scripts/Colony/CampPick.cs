using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A click on the camp ground outside build mode picks the nearest colonist within reach of the point, else
    /// the module whose footprint holds it, else clears the pick. The card names what was picked: a colonist's task,
    /// mood and wound, or a module's health and its build or repair state.
    /// </summary>
    public static class CampPick
    {
        public const float Reach = 1.3f;

        public static int Nearest(float px, float pz, IList<float> xs, IList<float> zs, float reach)
        {
            if (xs == null || zs == null) return -1;
            int best = -1;
            float bestSqr = reach * reach;
            int count = xs.Count < zs.Count ? xs.Count : zs.Count;
            for (int i = 0; i < count; i++)
            {
                float dx = xs[i] - px;
                float dz = zs[i] - pz;
                float sqr = dx * dx + dz * dz;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }
            return best;
        }

        public static string Mate(Survivor survivor, string language)
        {
            if (survivor == null) return "";
            string mood = ColonyDay.Mood(survivor.morale, survivor.trait, survivor.aside, survivor.mark);
            string line = survivor.displayName + "  " + OutpostZero.Shell.Loc.Task(survivor.task, language)
                + "  " + OutpostZero.Shell.Loc.Mood(mood, language);
            string wound = WoundCard.Line(survivor.injury, language);
            return wound.Length > 0 ? line + "  " + wound : line;
        }

        public static string Module(PlacedModule module, string language)
        {
            if (module == null) return "";
            string line = YardSay.Kind(module.kind, language) + "  " + module.integrity + "%";
            if (module.site != 0)
                return line + "  " + Word("pick.site", language) + " " + module.hours + "/" + BuildSite.Need(module.kind);
            if (module.integrity <= 0) return line + "  " + Word("pick.wrecked", language);
            if (module.integrity < 100) return line + "  " + Word("pick.worn", language);
            return line;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}

using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The next leader is the healthiest person with the strongest claim.
    /// Leadership is the days they have held the gate. The fourth day is the first that cuts a feud by one more.
    /// </summary>
    public static class Heir
    {
        public static int Pick(bool[] alive, int[] injury, int[] leadership, float[] morale)
        {
            int open = Best(alive, injury, leadership, morale, true);
            if (open >= 0) return open;
            return Best(alive, injury, leadership, morale, false);
        }

        public static string Line(int leadership, string language)
        {
            if (leadership <= 0) return "";
            return Loc.T("camp.leads", language) + " " + leadership;
        }

        private static int Best(bool[] alive, int[] injury, int[] leadership, float[] morale, bool healthy)
        {
            if (alive == null) return -1;
            int best = -1;
            float bestScore = 0f;
            for (int i = 0; i < alive.Length; i++)
            {
                if (!alive[i]) continue;
                int wound = injury != null && i < injury.Length ? injury[i] : 0;
                if (healthy && wound > 0) continue;
                int skill = leadership != null && i < leadership.Length ? leadership[i] : 0;
                if (skill < 0) skill = 0;
                float mood = morale != null && i < morale.Length ? morale[i] : 0f;
                float score = skill + mood;
                if (best >= 0 && score <= bestScore) continue;
                best = i;
                bestScore = score;
            }
            return best;
        }
    }
}

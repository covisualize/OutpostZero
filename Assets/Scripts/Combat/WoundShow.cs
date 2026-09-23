namespace OutpostZero.Combat
{
    /// <summary>
    /// A headshot mists. A bleed leaves a drip. A sprint or a wet street kicks up more at the feet.
    /// Gore off keeps the blood quiet. A crouch stays low.
    /// </summary>
    public static class WoundShow
    {
        public const float DripGap = 0.85f;
        public const float Mist = 0.46f;
        public const float Splash = 0.22f;
        public const int SprintPuffs = 6;
        public const int WalkPuffs = 2;
        public const int WetPuffs = 5;
        public const int WetSprintPuffs = 7;

        public static bool MistDue(bool crit, int gore)
        {
            return crit && gore > 0;
        }

        public static bool Bleeds(int gore)
        {
            return gore > 0;
        }

        public static bool DripDue(float now, float last)
        {
            if (last <= 0f) return true;
            if (now < last) return true;
            return now - last >= DripGap;
        }

        public static bool Soaked(float wetness)
        {
            return wetness >= 0.5f;
        }

        public static int Puffs(bool sprint, bool crouch, bool wet)
        {
            if (crouch) return 0;
            if (wet) return sprint ? WetSprintPuffs : WetPuffs;
            return sprint ? SprintPuffs : WalkPuffs;
        }
    }
}

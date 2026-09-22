using OutpostZero.AI;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A night already on the calendar still comes. An odd night comes early when the day
    /// was loud near the gate, or the generator is running, and the wall is too thin for the difficulty.
    /// </summary>
    public static class RaidCall
    {
        public const float HearRadius = 18f;
        public const int Cap = 24;
        public const int SurvivorShots = 3;
        public const int ScavengerShots = 6;
        public const int NightmareShots = 1;
        public const int WallHold = 2;

        public static bool Likely(int day, int security, bool endless, int shots, bool generator, int walls, int difficulty)
        {
            if (RaidPlan.Due(day, security, endless)) return true;
            if (day < 2 || security >= 8) return false;
            if (shots < 0) shots = 0;
            if (walls < 0) walls = 0;
            int level = DifficultyProfile.Resolve(difficulty);
            int need = level == 1 ? ScavengerShots : level == 3 ? NightmareShots : SurvivorShots;
            bool loud = shots >= need || (generator && level >= 2);
            if (!loud) return false;
            int hold = level == 3 ? WallHold + 1 : WallHold;
            return walls < hold;
        }

        public static int Hear(int shots, float x, float z, bool gun, bool loud)
        {
            shots = shots < 0 ? 0 : shots;
            if (!gun) return shots;
            RaidPlan.AnchorOf("gate", out float gx, out float gz);
            float dx = x - gx;
            float dz = z - gz;
            if (dx * dx + dz * dz > HearRadius * HearRadius) return shots;
            int next = shots + (loud ? 2 : 1);
            return next > Cap ? Cap : next;
        }

        public static int Carry(int shots, int fromDay, int toDay)
        {
            if (fromDay != toDay) return 0;
            if (shots < 0) return 0;
            return shots > Cap ? Cap : shots;
        }
    }
}

namespace OutpostZero.Colony
{
    /// <summary>
    /// Which side hits the yard, how hard, and whether the night is likely to bring them.
    /// </summary>
    public static class RaidPlan
    {
        public struct Wave
        {
            public string Approach;
            public int Pressure;
            public float Interval;
        }

        public static bool Due(int day, int security)
        {
            return Due(day, security, false);
        }

        public static bool Due(int day, int security, bool endless)
        {
            if (security >= 8) return false;
            if (!endless)
            {
                if (day < 2) return false;
                return day % 2 == 0;
            }
            return day >= 1;
        }

        public static Wave Opening(int day, int towers)
        {
            if (day < 1) day = 1;
            if (towers < 0) towers = 0;
            string[] sides = { "gate", "alley", "yard", "fence" };
            int index = ((day - 1) + towers) % 4;
            int pressure = 6 + day / 2 - towers * 2;
            if (pressure < 2) pressure = 2;
            float interval = 1.2f + towers * 0.4f;
            if (interval > 2.4f) interval = 2.4f;
            return new Wave { Approach = sides[index], Pressure = pressure, Interval = interval };
        }

        public static int SpawnCount(int day, int towers)
        {
            if (day < 1) day = 1;
            if (towers < 0) towers = 0;
            int count = 8 + day / 3 - towers * 3;
            if (count < 3) count = 3;
            if (count > 16) count = 16;
            return count;
        }

        public static int Strike(int pressure, int guards, int cover)
        {
            if (guards < 0) guards = 0;
            if (cover < 0) cover = 0;
            int hit = pressure - guards * 3 - cover;
            if (hit < 1) return 1;
            return hit;
        }

        public static void AnchorOf(string approach, out float x, out float z)
        {
            if (approach == "alley")
            {
                x = -20f;
                z = -12f;
                return;
            }
            if (approach == "yard")
            {
                x = -12f;
                z = -22f;
                return;
            }
            if (approach == "fence")
            {
                x = -12f;
                z = -2f;
                return;
            }
            x = -6f;
            z = -8f;
        }

        public static bool Covers(float anchorX, float anchorZ, float x, float z)
        {
            float dx = x - anchorX;
            float dz = z - anchorZ;
            return dx * dx + dz * dz <= 36f;
        }

        public static int PhaseAt(float elapsed, float duration)
        {
            if (duration <= 0f || elapsed <= 0f) return 0;
            int phase = (int)(elapsed / (duration / 3f));
            if (phase < 0) return 0;
            if (phase > 2) return 2;
            return phase;
        }

        public static Wave WaveAt(int day, int towers, int phase)
        {
            Wave open = Opening(day, towers);
            if (phase <= 0) return open;
            if (phase > 2) phase = 2;
            string[] sides = { "gate", "alley", "yard", "fence" };
            int start = 0;
            for (int i = 0; i < sides.Length; i++)
            {
                if (sides[i] == open.Approach) start = i;
            }
            int pressure = open.Pressure + phase * 2;
            if (pressure > 14) pressure = 14;
            float interval = open.Interval - phase * 0.2f;
            if (interval < 0.7f) interval = 0.7f;
            return new Wave
            {
                Approach = sides[(start + phase) % sides.Length],
                Pressure = pressure,
                Interval = interval
            };
        }

        public static int Fronts(int day, int difficulty)
        {
            if (day < 2) return 1;
            if (difficulty == 3 || day >= 8) return 3;
            return 2;
        }

        public static string Side(int day, int towers, int offset)
        {
            Wave open = Opening(day, towers);
            if (offset <= 0) return open.Approach;
            string[] sides = { "gate", "alley", "yard", "fence" };
            int start = 0;
            for (int i = 0; i < sides.Length; i++)
            {
                if (sides[i] == open.Approach) start = i;
            }
            if (offset > 3) offset = 3;
            return sides[(start + offset) % sides.Length];
        }

        public static int Share(int total, int fronts, int index)
        {
            if (total < 1) total = 1;
            if (fronts < 1) fronts = 1;
            if (fronts > 3) fronts = 3;
            if (index < 0 || index >= fronts) return 0;
            int each = total / fronts;
            int extra = total % fronts;
            return each + (index < extra ? 1 : 0);
        }

        public static int Reinforcements(int phase)
        {
            if (phase <= 0) return 0;
            if (phase == 1) return 3;
            return 4;
        }

        public static int Hurt(int injury)
        {
            if (injury < 0) injury = 0;
            if (injury >= 3) return 3;
            return injury + 1;
        }

        public static int Pick(bool[] alive, bool[] guard, int salt)
        {
            if (alive == null || alive.Length == 0) return -1;
            if (salt < 0) salt = 0;
            int guards = Count(alive, guard, true);
            if (guards > 0) return Nth(alive, guard, true, salt % guards);
            int living = Count(alive, null, false);
            if (living <= 0) return -1;
            return Nth(alive, null, false, salt % living);
        }

        private static int Count(bool[] alive, bool[] guard, bool onlyGuards)
        {
            int count = 0;
            for (int i = 0; i < alive.Length; i++)
            {
                if (!alive[i]) continue;
                if (onlyGuards && (guard == null || i >= guard.Length || !guard[i])) continue;
                count++;
            }
            return count;
        }

        private static int Nth(bool[] alive, bool[] guard, bool onlyGuards, int nth)
        {
            int seen = 0;
            for (int i = 0; i < alive.Length; i++)
            {
                if (!alive[i]) continue;
                if (onlyGuards && (guard == null || i >= guard.Length || !guard[i])) continue;
                if (seen == nth) return i;
                seen++;
            }
            return -1;
        }
    }
}

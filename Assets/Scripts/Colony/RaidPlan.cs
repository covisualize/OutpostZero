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
    }
}

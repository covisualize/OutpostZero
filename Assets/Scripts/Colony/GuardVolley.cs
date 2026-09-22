namespace OutpostZero.Colony
{
    /// <summary>
    /// Guards on duty fire together during a raid. Three is the most that shoot at once.
    /// </summary>
    public static class GuardVolley
    {
        public const float Interval = 1.4f;
        public const float Range = 16f;
        public const float Damage = 8f;
        public const int Cap = 3;

        public static bool Ready(float since, int guards)
        {
            if (guards <= 0 || since < 0f) return false;
            return since >= Interval;
        }

        public static int Crew(int guards)
        {
            if (guards < 0) return 0;
            return guards > Cap ? Cap : guards;
        }

        public static float Hit(int guards)
        {
            return Damage * Crew(guards);
        }

        public static int Rounds(int guards, int stored)
        {
            int crew = Crew(guards);
            if (stored < 0) stored = 0;
            return stored < crew ? stored : crew;
        }

        public static float Fired(int guards, int stored)
        {
            return Damage * Rounds(guards, stored);
        }

        public static int Brought(bool scrounger)
        {
            return scrounger ? 4 : 2;
        }

        public static int Pick(float[] distance)
        {
            if (distance == null) return -1;
            int best = -1;
            float near = Range;
            for (int i = 0; i < distance.Length; i++)
            {
                if (distance[i] < 0f || distance[i] > near) continue;
                near = distance[i];
                best = i;
            }
            return best;
        }
    }
}

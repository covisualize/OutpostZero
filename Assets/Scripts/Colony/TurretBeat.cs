namespace OutpostZero.Colony
{
    /// <summary>
    /// A placed turret, with the generator running and a round to spend, shoots the nearest
    /// zombie inside its range. Rifle rounds are spent before any other gun.
    /// </summary>
    public static class TurretBeat
    {
        public const float Interval = 0.85f;
        public const float Range = 22f;
        public const float Damage = 18f;

        public static bool Ready(float since, bool powered, int guns, int rounds)
        {
            if (!powered || guns <= 0 || rounds <= 0) return false;
            if (since < 0f) return false;
            return since >= Interval;
        }

        public static int Pick(float[] distance, float range)
        {
            if (distance == null) return -1;
            int best = -1;
            float near = range;
            for (int i = 0; i < distance.Length; i++)
            {
                if (distance[i] < 0f || distance[i] > near) continue;
                near = distance[i];
                best = i;
            }
            return best;
        }

        public static int Prefer(bool[] rifle, int[] rounds)
        {
            if (rounds == null) return -1;
            int any = -1;
            for (int i = 0; i < rounds.Length; i++)
            {
                if (rounds[i] <= 0) continue;
                bool isRifle = rifle != null && i < rifle.Length && rifle[i];
                if (isRifle) return i;
                if (any < 0) any = i;
            }
            return any;
        }
    }
}

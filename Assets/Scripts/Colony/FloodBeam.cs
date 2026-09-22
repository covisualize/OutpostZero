namespace OutpostZero.Colony
{
    /// <summary>
    /// A floodlight washes a circle when the generator is running. Inside that circle a crouch is no longer dark.
    /// </summary>
    public static class FloodBeam
    {
        public const float Radius = 14f;

        public static float Strength(float distance, bool powered)
        {
            if (!powered) return 0f;
            if (distance < 0f) distance = 0f;
            if (distance >= Radius) return 0f;
            return 1f - distance / Radius;
        }

        public static int Covering(string approach, float[] x, float[] z, int[] sites, int[] integrity, bool powered)
        {
            if (!powered || x == null || z == null) return 0;
            RaidPlan.AnchorOf(approach, out float anchorX, out float anchorZ);
            int count = x.Length < z.Length ? x.Length : z.Length;
            int lit = 0;
            float reach = Radius * Radius;
            for (int i = 0; i < count; i++)
            {
                if (sites != null && (i >= sites.Length || sites[i] != 0)) continue;
                if (integrity != null && (i >= integrity.Length || integrity[i] <= 0)) continue;
                float dx = x[i] - anchorX;
                float dz = z[i] - anchorZ;
                if (dx * dx + dz * dz >= reach) continue;
                lit++;
            }
            return lit;
        }

        public static int ApproachPressure(int pressure, int lamps)
        {
            if (pressure < 1) pressure = 1;
            if (lamps < 0) lamps = 0;
            if (lamps > 2) lamps = 2;
            int next = pressure - lamps * 2;
            if (next < 1) return 1;
            return next;
        }

        public static float ApproachGap(float interval, int lamps)
        {
            if (interval < 0f) interval = 0f;
            if (lamps < 0) lamps = 0;
            if (lamps > 2) lamps = 2;
            float next = interval + lamps * 0.35f;
            if (next > 3.1f) return 3.1f;
            return next;
        }
    }
}

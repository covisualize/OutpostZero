namespace OutpostZero.Graphics
{
    /// <summary>
    /// A step in a rain puddle carries farther. A crouch keeps the quiet radius.
    /// Puddles only exist when the street is wet enough to show them.
    /// </summary>
    public static class PuddleStep
    {
        public const float Splash = 1.45f;

        public static bool Inside(float x, float z, float wetness)
        {
            if (!RainPuddle.Shows(wetness)) return false;
            for (int i = 0; i < RainPuddle.Count; i++)
            {
                var spot = RainPuddle.At(i);
                float halfW = spot.W * 0.5f;
                float halfD = spot.D * 0.5f;
                if (halfW <= 0f || halfD <= 0f) continue;
                float nx = (x - spot.X) / halfW;
                float nz = (z - spot.Z) / halfD;
                if (nx * nx + nz * nz <= 1f) return true;
            }
            return false;
        }

        public static float Radius(float radius, bool inside, bool crouching)
        {
            if (radius < 0f) radius = 0f;
            if (!inside || crouching) return radius;
            return radius * Splash;
        }
    }
}

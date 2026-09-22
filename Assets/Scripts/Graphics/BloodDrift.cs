namespace OutpostZero.Graphics
{
    /// <summary>
    /// Blood travels with the shot across the ground. A straight-down hit leaves no streak.
    /// Gore off stays clean.
    /// </summary>
    public static class BloodDrift
    {
        public const float Reach = 0.55f;
        public const int Drops = 3;

        public static bool Shows(int gore, bool blood)
        {
            return blood && gore > 0;
        }

        public static void Along(float dx, float dy, float dz, out float ox, out float oy, out float oz)
        {
            ox = 0f;
            oy = 0f;
            oz = 0f;
            float flat = dx * dx + dz * dz;
            if (flat < 0.0001f) return;
            float scale = Reach / (float)System.Math.Sqrt(flat);
            ox = dx * scale;
            oz = dz * scale;
        }
    }
}

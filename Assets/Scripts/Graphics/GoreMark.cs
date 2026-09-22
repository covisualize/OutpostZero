namespace OutpostZero.Graphics
{
    /// <summary>
    /// How a hit marks the street. Gore off drops blood. A shotgun sprays.
    /// Marks shrink over the last two seconds so they do not pop off.
    /// </summary>
    public static class GoreMark
    {
        public const float Cull = 40f;
        public const float Fade = 2f;
        public const float SpreadTime = 3f;

        public static int Splats(int level, bool shotgun, bool blood)
        {
            if (level < 0) level = 1;
            if (blood && level <= 0) return 0;
            if (!shotgun) return 1;
            if (level >= 2) return 5;
            return 3;
        }

        public static float Size(string kind, int level)
        {
            float size = kind == "scorch" ? 0.7f : kind == "blood" ? 0.42f : 0.28f;
            if (kind == "blood" && level >= 2) size *= 1.35f;
            return size;
        }

        public static bool Near(float dx, float dy, float dz)
        {
            float reach = Cull * Cull;
            return dx * dx + dy * dy + dz * dz <= reach;
        }

        public static float FadeScale(float remaining, float full)
        {
            if (full < 0f) full = 0f;
            if (remaining >= Fade) return full;
            if (remaining <= 0f) return 0f;
            return full * (remaining / Fade);
        }

        public static float Spread(float age, float full)
        {
            if (full < 0f) full = 0f;
            if (age < 0f) age = 0f;
            if (age >= SpreadTime) return full;
            return full * (0.35f + 0.65f * (age / SpreadTime));
        }

        public static void Offset(int index, out float x, out float y)
        {
            if (index == 1) { x = 0.18f; y = 0.08f; return; }
            if (index == 2) { x = -0.16f; y = 0.12f; return; }
            if (index == 3) { x = 0.05f; y = -0.18f; return; }
            if (index == 4) { x = -0.22f; y = -0.06f; return; }
            x = 0f;
            y = 0f;
        }
    }
}

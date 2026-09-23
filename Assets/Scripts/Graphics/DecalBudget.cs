using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// How the street's marks recycle. Past the tier's budget the oldest mark fades out over two
    /// seconds instead of vanishing, and new marks borrow a small overflow while it goes.
    /// </summary>
    public static class DecalBudget
    {
        public const int Ceiling = 400;
        public const float Fade = 2f;

        public static int Cap(int tierDecals)
        {
            if (tierDecals < 1) tierDecals = 1;
            return tierDecals > Ceiling ? Ceiling : tierDecals;
        }

        /// <summary>Projectors allowed beyond the cap while retired marks finish fading.</summary>
        public static int Slack(int cap)
        {
            int slack = cap / 8;
            return slack < 8 ? 8 : slack;
        }

        public static bool CanGrow(int pooled, int cap)
        {
            return pooled < cap + Slack(cap);
        }

        public static bool OverBudget(int live, int cap)
        {
            return live >= cap;
        }

        /// <summary>Opacity of a retiring mark <paramref name="elapsed"/> seconds after it was retired.</summary>
        public static float Retire(float elapsed)
        {
            if (elapsed <= 0f) return 1f;
            if (elapsed >= Fade) return 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, elapsed / Fade);
        }

        public static bool Retired(float elapsed)
        {
            return elapsed >= Fade;
        }
    }

    /// <summary>
    /// Blood that carries past the body onto the wall behind it: one splat for most kills, a
    /// cone of three to five for a shotgun, with a drip running down a standing wall.
    /// </summary>
    public static class BackSplat
    {
        public const float Reach = 3f;
        public const float Cone = 14f;
        public const float DripDrop = 0.32f;

        public static int Count(int gore, bool shotgun)
        {
            if (gore <= 0) return 0;
            return GoreMark.Splats(gore, shotgun, true);
        }

        /// <summary>Yaw and pitch, in degrees, of splat <paramref name="index"/> around the shot line.</summary>
        public static void Spray(int index, int count, out float yaw, out float pitch)
        {
            if (index <= 0 || count <= 1)
            {
                yaw = 0f;
                pitch = 0f;
                return;
            }
            float angle = (index - 1) * (360f / (count - 1)) * Mathf.Deg2Rad;
            yaw = Mathf.Cos(angle) * Cone;
            pitch = Mathf.Sin(angle) * Cone * 0.6f;
        }

        public static bool Drips(float normalY)
        {
            return Mathf.Abs(normalY) < 0.5f;
        }
    }

    /// <summary>An explosion leaves a scorch the width of its blast on the ground beneath it.</summary>
    public static class BlastScorch
    {
        public const float Size = 5f;
        public const float Probe = 4f;

        public static float SizeFor(float radius)
        {
            return radius > Size ? radius : Size;
        }
    }
}

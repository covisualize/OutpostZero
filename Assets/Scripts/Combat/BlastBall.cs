using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A powder blast opens a four-frame fireball and a warp ring.
    /// Toxic and oil stay with the cloud and the slick.
    /// The wake, the fuse, and the damage stay as they were.
    /// </summary>
    public static class BlastBall
    {
        public const int Frames = 4;
        public const float Life = 0.48f;
        public const float Start = 0.6f;
        public const float End = 3.4f;
        public const float Warp = 6.2f;
        public const float WarpTime = 0.36f;

        public static bool Shows(HazardKind kind)
        {
            return kind == HazardKind.Explosive;
        }

        public static float Scale(float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Start + (End - Start) * t;
        }

        public static int Frame(float t)
        {
            if (t < 0f) t = 0f;
            if (t >= 1f) return Frames - 1;
            int frame = (int)(t * Frames);
            if (frame < 0) frame = 0;
            if (frame > Frames - 1) frame = Frames - 1;
            return frame;
        }

        public static float WarpScale(float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return 0.4f + Warp * t;
        }
    }
}

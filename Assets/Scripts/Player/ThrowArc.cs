using System;

namespace OutpostZero.Player
{
    /// <summary>
    /// A bottle leaves the hand fast enough to land past fifteen meters and breaks loud enough to pull a group.
    /// </summary>
    public static class ThrowArc
    {
        public const float Forward = 12f;
        public const float Lift = 5.4f;
        public const float Height = 1.4f;
        public const float Gravity = 9.81f;
        public const float LureRadius = 18f;
        public const float FireRadius = 18f;

        public static float Flight(float height, float forward, float lift, float gravity)
        {
            if (gravity < 0.1f) gravity = 9.81f;
            if (height < 0f) height = 0f;
            if (forward < 0f) forward = 0f;
            float disc = lift * lift + 2f * gravity * height;
            if (disc < 0f) disc = 0f;
            float time = (lift + (float)Math.Sqrt(disc)) / gravity;
            if (time < 0f) time = 0f;
            return forward * time;
        }

        /// <summary>
        /// Fills the preview line: <paramref name="along"/> metres ahead of the hand and <paramref name="up"/>
        /// metres above the ground, evenly spaced in time from the release to the landing. Returns the points written.
        /// </summary>
        public static int Sample(float height, float forward, float lift, float gravity, float[] along, float[] up)
        {
            if (along == null || up == null) return 0;
            int count = Math.Min(along.Length, up.Length);
            if (count < 2) return 0;
            if (gravity < 0.1f) gravity = 9.81f;
            if (height < 0f) height = 0f;
            if (forward < 0f) forward = 0f;
            float disc = lift * lift + 2f * gravity * height;
            float landing = (lift + (float)Math.Sqrt(disc < 0f ? 0f : disc)) / gravity;
            for (int i = 0; i < count; i++)
            {
                float t = landing * i / (count - 1);
                along[i] = forward * t;
                float y = height + lift * t - 0.5f * gravity * t * t;
                up[i] = y < 0f ? 0f : y;
            }
            return count;
        }

        public static float NoiseRadius(bool molotov)
        {
            return molotov ? FireRadius : LureRadius;
        }
    }
}

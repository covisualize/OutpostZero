using OutpostZero.Core;

namespace OutpostZero.Sensory
{
    /// <summary>
    /// A rain bolt speaks late. While the thunder rolls, a gunshot does not call the dead.
    /// An explosion, a step, and a scream still carry.
    /// </summary>
    public static class StormCover
    {
        public const float Delay = 0.6f;
        public const float Cover = 1.4f;
        public const float Volume = 0.7f;
        public const float Radius = 40f;

        public static float Bolt { get; private set; }

        public static void Strike(float now)
        {
            Bolt = now;
        }

        public static bool ThunderDue(float bolt, float now, bool played)
        {
            if (played || bolt <= 0f) return false;
            if (now < bolt) return false;
            return now - bolt >= Delay;
        }

        public static bool Masks(float bolt, float now, NoiseType type)
        {
            if (bolt <= 0f) return false;
            if (type != NoiseType.GunshotLoud && type != NoiseType.GunshotQuiet) return false;
            float start = bolt + Delay;
            if (now < start) return false;
            return now < start + Cover;
        }
    }
}

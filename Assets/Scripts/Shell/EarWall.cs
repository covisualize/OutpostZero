using OutpostZero.Sensory;

namespace OutpostZero.Shell
{
    /// <summary>
    /// A wall between the ear and a world sound muffles it.
    /// A scream stops. Everything else leaks. Music and the rain are not in the world.
    /// </summary>
    public static class EarWall
    {
        public const float MuffledHz = 1400f;
        public const float Clear = 1.2f;

        public static float Gain(float volume, bool wall, bool scream)
        {
            if (volume < 0f) volume = 0f;
            if (volume > 1f) volume = 1f;
            if (!wall) return volume;
            if (scream) return 0f;
            return volume * HearGate.Leak;
        }

        public static float Muffle(float openHz)
        {
            if (openHz < MuffledHz) return openHz;
            return MuffledHz;
        }

        public static bool InWorld(float spatial)
        {
            return spatial >= 1f;
        }
    }
}

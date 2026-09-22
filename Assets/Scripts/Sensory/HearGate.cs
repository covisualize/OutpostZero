using OutpostZero.Core;

namespace OutpostZero.Sensory
{
    /// <summary>
    /// A scream stops at a solid wall. Other noise still leaks. An open gap passes the full sound.
    /// </summary>
    public static class HearGate
    {
        public const float Leak = 0.45f;
        public const float Floor = 0.05f;

        public static float Perceived(float distance, float radius, float intensity, bool wall, NoiseType type)
        {
            if (radius <= 0.01f) return 0f;
            if (intensity < 0f) intensity = 0f;
            if (distance < 0f) distance = 0f;
            if (distance > radius) return 0f;
            float occlusion = 1f;
            if (wall) occlusion = type == NoiseType.ZombieScream ? 0f : Leak;
            float perceived = (1f - distance / radius) * intensity * occlusion;
            if (perceived <= Floor) return 0f;
            return perceived;
        }
    }
}

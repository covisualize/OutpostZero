using OutpostZero.Core;

namespace OutpostZero.Sensory
{
    /// <summary>
    /// A subtitle prints only when the sound would reach the ear.
    /// Thunder is quiet to the dead, and still loud to the player inside its radius.
    /// A wall stops a scream. Rain can drop a faint sound under the floor.
    /// </summary>
    public static class CaptionGate
    {
        public static bool Show(float distance, float radius, bool wall, NoiseType type, bool rain)
        {
            if (type == NoiseType.Thunder)
            {
                if (radius <= 0f) return false;
                if (distance < 0f) distance = 0f;
                return distance <= radius;
            }
            float heard = RainMask.Heard(HearGate.Perceived(distance, radius, 1f, wall, type), rain);
            return heard > 0f;
        }
    }
}

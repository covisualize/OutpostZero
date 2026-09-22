namespace OutpostZero.Player
{
    /// <summary>
    /// How far a walker can see. A crouch in full dark stays hidden at five meters.
    /// The flashlight forces full exposure, so the same five meters is a spot.
    /// </summary>
    public static class SpotRange
    {
        public static float Exposure(bool crouch, bool sprint, bool flashlight, float night, float lamp)
        {
            if (flashlight) return 1f;
            float value = 0.5f;
            if (crouch) value = 0.22f;
            else if (sprint) value = 0.85f;
            if (night < 0f) night = 0f;
            if (night > 1f) night = 1f;
            if (lamp < 0f) lamp = 0f;
            if (lamp > 1f) lamp = 1f;
            if (night > 0.45f)
            {
                float dark = value * 0.35f + lamp * 0.7f;
                value = value + (dark - value) * night;
            }
            else value += lamp * 0.15f;
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        public static float Meters(float sight, float exposure, bool crouch, float weather, float cover)
        {
            if (sight < 0f) sight = 0f;
            float level = exposure < 0f ? 0f : exposure > 1f ? 1f : exposure;
            float range = sight * (0.35f + 0.85f * level);
            if (crouch) range *= 0.75f;
            range *= weather;
            range *= cover;
            return range < 0f ? 0f : range;
        }

        public static bool Notices(float distance, float sight, float exposure, bool crouch, float weather, float cover, float angle, float sightAngle)
        {
            if (distance < 0f) distance = 0f;
            if (distance > Meters(sight, exposure, crouch, weather, cover)) return false;
            return angle <= sightAngle * 0.5f;
        }

        public static bool Beam(float distance, float facingAngle, bool flashlight)
        {
            return flashlight && distance <= 12f && facingAngle <= 20f;
        }
    }
}

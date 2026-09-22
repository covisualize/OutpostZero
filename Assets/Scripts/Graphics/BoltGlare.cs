namespace OutpostZero.Graphics
{
    /// <summary>
    /// A lightning strike lights the street for a short beat.
    /// A crouch in the dark is easier to see until the flash dies.
    /// A flashlight is already fully lit, so the bolt does not add more.
    /// </summary>
    public static class BoltGlare
    {
        public const float Hold = 0.55f;
        public const float Lift = 0.42f;
        public const float Flash = 0.85f;
        public const float Mix = 0.45f;

        public static bool Live(float bolt, float now)
        {
            if (bolt <= 0f) return false;
            if (now < bolt) return false;
            return now - bolt <= Hold + OutpostZero.Core.Tick.Slack;
        }

        public static float Glare(float exposure, bool live)
        {
            if (exposure < 0f) exposure = 0f;
            if (exposure > 1f) exposure = 1f;
            if (!live) return exposure;
            float next = exposure + Lift;
            return next > 1f ? 1f : next;
        }

        public static float Bright(float grade, bool live)
        {
            if (!live) return grade;
            return grade + Flash;
        }

        public static void Wash(bool live, float red, float green, float blue, out float r, out float g, out float b)
        {
            if (!live)
            {
                r = red;
                g = green;
                b = blue;
                return;
            }
            r = red + (1f - red) * Mix;
            g = green + (1f - green) * Mix;
            b = blue + (1f - blue) * Mix;
        }
    }
}

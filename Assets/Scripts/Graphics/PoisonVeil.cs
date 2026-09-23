namespace OutpostZero.Graphics
{
    /// <summary>
    /// Poison closes the vignette and softens the street. A clear body keeps the grade it already had.
    /// </summary>
    public static class PoisonVeil
    {
        public const float Vignette = 0.24f;
        public const float Cap = 0.78f;
        public const float Blur = 0.62f;
        public const float ClearBlur = 0.35f;

        public static float Shade(float grade, bool poisoned)
        {
            if (grade < 0f) grade = 0f;
            if (!poisoned) return grade;
            float next = grade + Vignette;
            return next > Cap ? Cap : next;
        }

        public static bool Soft(bool poisoned, bool motion)
        {
            return poisoned || motion;
        }

        public static float BlurOf(bool poisoned, bool motion)
        {
            if (poisoned) return Blur;
            if (motion) return ClearBlur;
            return 0f;
        }

        public static void Tint(bool poisoned, float red, float green, float blue, out float r, out float g, out float b)
        {
            if (!poisoned)
            {
                r = red;
                g = green;
                b = blue;
                return;
            }
            r = red * 0.72f;
            g = green * 0.95f;
            b = blue * 0.62f;
        }
    }
}

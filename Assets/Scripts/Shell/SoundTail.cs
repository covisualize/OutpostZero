namespace OutpostZero.Shell
{
    /// <summary>
    /// A shot close to the ear is only the crack. Farther out, a lower tail follows it.
    /// An explosion does the same once it is past the near blast.
    /// </summary>
    public static class SoundTail
    {
        public const float GunNear = 12f;
        public const float GunFar = 32f;
        public const float EchoNear = 18f;
        public const float EchoFar = 48f;

        public static float Gun(float distance)
        {
            return Rise(distance, GunNear, GunFar, 0.35f);
        }

        public static float Echo(float distance)
        {
            return Rise(distance, EchoNear, EchoFar, 0.4f);
        }

        public static float Pitch(float distance)
        {
            if (distance < GunNear) return 1f;
            float span = GunFar - GunNear;
            float t = (distance - GunNear) / span;
            if (t > 1f) t = 1f;
            return 1f - 0.38f * t;
        }

        private static float Rise(float distance, float near, float far, float peak)
        {
            if (distance < near) return 0f;
            float span = far - near;
            if (span <= 0f) return peak;
            float t = (distance - near) / span;
            if (t > 1f) t = 1f;
            return peak * t;
        }
    }
}

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Paper slides along the street with the wind. A clear day leaves the sheets down.
    /// </summary>
    public static class WindSheet
    {
        public const int Count = 5;
        public const float Speed = 3.2f;
        public const float Span = 22f;

        public static bool Skims(WeatherKind kind)
        {
            return kind == WeatherKind.Rain || kind == WeatherKind.Storm
                || kind == WeatherKind.Fog || kind == WeatherKind.Overcast;
        }

        public static void Home(int index, out float x, out float z)
        {
            int i = index < 0 ? 0 : index % Count;
            x = -18f + i * 9f;
            z = 6f + (i % 2) * 3.2f + (i / 2) * 0.4f;
        }

        public static void Step(float x, float z, float wind, float dt, out float nx, out float nz)
        {
            if (wind < 0f) wind = 0f;
            if (dt < 0f) dt = 0f;
            nx = x + Speed * wind * dt;
            nz = z;
            float width = Span * 2f;
            if (nx > Span)
            {
                nx -= width;
                if (nx > Span) nx = -Span;
            }
        }

        public static float Tilt(float time, int index, float wind)
        {
            if (wind < 0f) wind = 0f;
            int i = index < 0 ? 0 : index;
            double wave = System.Math.Sin(time * 5.0 + i * 1.3);
            return (float)(wave * 22.0 * wind);
        }
    }
}

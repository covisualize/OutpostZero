using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>
    /// The wide flashlight wears a round cookie with a dirt ring.
    /// The center stays bright. The ring sits at 0.72 of the radius and dips the light.
    /// </summary>
    public static class LampCookie
    {
        public const int Size = 256;
        public const float Inner = 40f;
        public const float Outer = 62f;
        public const float Ring = 0.72f;
        public const float Band = 0.05f;
        public const float Nick = 0.35f;

        public static float Shade(float u, float v)
        {
            float dx = u - 0.5f;
            float dy = v - 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            if (r < 0f) r = 0f;
            float fall = 1f - r;
            if (fall < 0f) fall = 0f;
            float band = r - Ring;
            if (band < 0f) band = -band;
            if (band < Band)
            {
                float nick = 1f - band / Band;
                fall *= 1f - Nick * nick;
            }
            if (fall < 0f) return 0f;
            if (fall > 1f) return 1f;
            return fall;
        }

        public static Texture2D Bake()
        {
            var cookie = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            cookie.name = "LampCookie";
            cookie.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float shade = Shade((x + 0.5f) / Size, (y + 0.5f) / Size);
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, shade);
                }
            }
            cookie.SetPixels(pixels);
            cookie.Apply(false, true);
            return cookie;
        }
    }
}

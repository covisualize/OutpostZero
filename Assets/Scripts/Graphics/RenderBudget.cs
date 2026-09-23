using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// LOD transition heights for generated set pieces: full mesh inside <see cref="QualityProfile.LodSwitch"/>,
    /// the 50 % decimation out to <see cref="QualityProfile.LodCull"/>, then culled. Heights are measured at the
    /// default street FOV with a LOD bias of 1; the tier's bias stretches or shrinks the distances.
    /// </summary>
    public static class LodBands
    {
        public const float ReferenceFov = 55f;

        /// <summary>Unity's screen-relative height of an object of <paramref name="size"/> metres at a camera distance.</summary>
        public static float ScreenHeight(float size, float distance, float fovDegrees = ReferenceFov)
        {
            if (size <= 0f || distance <= 0f) return 1f;
            float halfTan = Mathf.Tan(Mathf.Clamp(fovDegrees, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
            return Mathf.Clamp(size * 0.5f / (distance * halfTan), 0.0001f, 1f);
        }

        /// <summary>The distance at which an object shrinks to a screen height; the inverse of <see cref="ScreenHeight"/>.</summary>
        public static float Distance(float size, float height, float fovDegrees = ReferenceFov)
        {
            if (size <= 0f || height <= 0f) return 0f;
            float halfTan = Mathf.Tan(Mathf.Clamp(fovDegrees, 1f, 179f) * 0.5f * Mathf.Deg2Rad);
            return size * 0.5f / (height * halfTan);
        }

        /// <summary>
        /// One strictly falling height per level. The last level ends at the cull distance; earlier levels
        /// split the switch-to-cull span evenly.
        /// </summary>
        public static float[] Heights(float size, int levels)
        {
            if (levels < 1) levels = 1;
            var heights = new float[levels];
            for (int i = 0; i < levels; i++)
            {
                float distance = levels == 1
                    ? QualityProfile.LodCull
                    : Mathf.Lerp(QualityProfile.LodSwitch, QualityProfile.LodCull, i / (float)(levels - 1));
                heights[i] = ScreenHeight(size, distance);
                if (i > 0 && heights[i] >= heights[i - 1]) heights[i] = heights[i - 1] * 0.5f;
            }
            return heights;
        }
    }

    /// <summary>
    /// What the occlusion bake treats as a wall. Static meshes that are big on two axes hide what is behind
    /// them; smaller static clutter only receives culling so it never punches holes in the bake.
    /// </summary>
    public static class OcclusionPlan
    {
        public const float SmallestOccluder = 3f;
        public const float SmallestHole = 0.25f;
        public const float BackfaceThreshold = 100f;

        public static bool Occludes(Vector3 size)
        {
            float a = Mathf.Abs(size.x), b = Mathf.Abs(size.y), c = Mathf.Abs(size.z);
            float largest = Mathf.Max(a, Mathf.Max(b, c));
            float smallest = Mathf.Min(a, Mathf.Min(b, c));
            float middle = a + b + c - largest - smallest;
            return largest >= SmallestOccluder && middle >= SmallestOccluder * 0.5f;
        }
    }

    public enum TextureRole
    {
        Color,
        Normal,
        Linear,
        Screen
    }

    /// <summary>
    /// Import rules for every texture the pipeline writes: mipmaps always, block compression per platform
    /// (BC7 or BC5 on desktop, ASTC on mobile), and mip streaming for textures that sit on renderers.
    /// Format numbers are TextureImporterFormat values so runtime code needs no editor reference.
    /// </summary>
    public static class TextureRules
    {
        public const int BC5 = 27;
        public const int BC7 = 25;
        public const int Astc4x4 = 48;
        public const int Astc6x6 = 50;

        public static bool Governs(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            path = path.Replace('\\', '/');
            if (!path.EndsWith(".png") && !path.EndsWith(".tga") && !path.EndsWith(".jpg")) return false;
            return path.StartsWith("Assets/Models/") || path.StartsWith("Assets/Materials/") || path.StartsWith("Assets/Textures/");
        }

        public static TextureRole RoleOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return TextureRole.Color;
            path = path.Replace('\\', '/');
            int dot = path.LastIndexOf('.');
            string stem = dot > 0 ? path.Substring(0, dot) : path;
            if (stem.EndsWith("_Normal")) return TextureRole.Normal;
            if (stem.EndsWith("_Icon") || path.StartsWith("Assets/Textures/Decals/")) return TextureRole.Screen;
            if (stem.EndsWith("_AO") || stem.EndsWith("_Mask")) return TextureRole.Linear;
            return TextureRole.Color;
        }

        public static int Desktop(TextureRole role) => role == TextureRole.Normal ? BC5 : BC7;

        public static int Mobile(TextureRole role) => role == TextureRole.Normal ? Astc4x4 : Astc6x6;

        /// <summary>Icons and decal projectors are drawn without a renderer, so streaming would park them on their lowest mip.</summary>
        public static bool Streams(TextureRole role) => role != TextureRole.Screen;
    }
}

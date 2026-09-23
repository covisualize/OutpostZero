#if UNITY_EDITOR
using UnityEditor;
using OutpostZero.Graphics;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Holds every pipeline texture to <see cref="TextureRules"/> on import, so a hand-edited or
    /// hand-dropped texture cannot ship uncompressed, without mips, or outside the streaming budget.
    /// </summary>
    public class TextureImportPolicy : AssetPostprocessor
    {
        private static readonly string[] MobileTargets = { "Android", "iPhone" };

        private void OnPreprocessTexture()
        {
            if (!TextureRules.Governs(assetPath)) return;
            Apply((TextureImporter)assetImporter, assetPath);
        }

        public static void Apply(TextureImporter importer, string path)
        {
            var role = TextureRules.RoleOf(path);
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = TextureRules.Streams(role);
            if (role == TextureRole.Normal) importer.textureType = TextureImporterType.NormalMap;

            var desktop = importer.GetPlatformTextureSettings("Standalone");
            desktop.overridden = true;
            desktop.format = (TextureImporterFormat)TextureRules.Desktop(role);
            desktop.maxTextureSize = importer.maxTextureSize;
            importer.SetPlatformTextureSettings(desktop);

            foreach (string target in MobileTargets)
            {
                var mobile = importer.GetPlatformTextureSettings(target);
                mobile.overridden = true;
                mobile.format = (TextureImporterFormat)TextureRules.Mobile(role);
                mobile.maxTextureSize = importer.maxTextureSize;
                importer.SetPlatformTextureSettings(mobile);
            }
        }
    }
}
#endif

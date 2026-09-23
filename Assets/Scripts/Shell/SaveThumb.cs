using System;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Renders the main camera into a small square for a save slot, without the interface on top,
    /// and turns the stored text back into a texture for the load screen.
    /// </summary>
    public static class SaveThumb
    {
        public static string Take()
        {
            var camera = Camera.main;
            if (camera == null || Application.isBatchMode) return "";
            int side = SaveStamp.ThumbSide;
            var target = RenderTexture.GetTemporary(side, side, 24, RenderTextureFormat.ARGB32);
            var held = camera.targetTexture;
            var active = RenderTexture.active;
            Texture2D picture = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                picture = new Texture2D(side, side, TextureFormat.RGB24, false);
                picture.ReadPixels(new Rect(0, 0, side, side), 0, 0);
                picture.Apply(false);
                return SaveStamp.Keep(Convert.ToBase64String(picture.EncodeToJPG(SaveStamp.ThumbQuality)));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SaveThumb] " + ex.Message);
                return "";
            }
            finally
            {
                camera.targetTexture = held;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                if (picture != null) UnityEngine.Object.Destroy(picture);
            }
        }

        public static Texture2D Read(string thumbnail)
        {
            if (string.IsNullOrEmpty(thumbnail)) return null;
            try
            {
                var picture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (picture.LoadImage(Convert.FromBase64String(thumbnail))) return picture;
                UnityEngine.Object.Destroy(picture);
            }
            catch (FormatException)
            {
            }
            return null;
        }
    }
}

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Gives every FBX material a real surface as it is imported. A Blender name that follows the
    /// Mat_Family_Variant convention takes the library's URP/Lit material (textures and all);
    /// anything else becomes URP/Lit built from the Principled values Blender exported (base colour,
    /// metallic in ReflectionFactor, roughness as Shininess = (1 - roughness) * 10, emission), so no
    /// import is magenta or default grey. Models with a baked set are still remapped onto their
    /// baked material by <see cref="FbxPrefabPostprocessor"/>, which wins over this.
    /// </summary>
    public class FbxMaterialPostprocessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return 10;
        }

        private void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (material == null || description == null) return;
            var family = MaterialLibrary.FamilyFor(description.materialName);
            var library = family != SurfaceFamily.None ? AssetDatabase.LoadAssetAtPath<Material>(MaterialLibrary.LitPath(family)) : null;
            if (library != null)
            {
                material.shader = library.shader;
                material.CopyPropertiesFromMaterial(library);
                return;
            }

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;
            material.shader = lit;
            Color baseColor = Color.white;
            if (description.TryGetProperty("DiffuseColor", out Vector4 diffuse)) baseColor = new Color(diffuse.x, diffuse.y, diffuse.z, 1f);
            if (description.TryGetProperty("DiffuseFactor", out float diffuseFactor)) baseColor *= diffuseFactor;
            baseColor.a = description.TryGetProperty("Opacity", out float opacity) ? opacity : 1f;
            material.SetColor("_BaseColor", baseColor);
            if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription albedo) && albedo.texture != null)
                material.SetTexture("_BaseMap", albedo.texture);

            float metallic = description.TryGetProperty("ReflectionFactor", out float reflection) ? reflection : 0f;
            float shininess = description.TryGetProperty("Shininess", out float shine) ? shine : 5f;
            material.SetFloat("_Metallic", ImportedSurface.Metallic(metallic));
            material.SetFloat("_Smoothness", ImportedSurface.Smoothness(shininess));

            if (description.TryGetProperty("EmissiveColor", out Vector4 emissive))
            {
                float factor = description.TryGetProperty("EmissiveFactor", out float emissiveFactor) ? emissiveFactor : 1f;
                var glow = new Color(emissive.x, emissive.y, emissive.z) * factor;
                if (ImportedSurface.Glows(glow.r, glow.g, glow.b))
                {
                    material.SetColor("_EmissionColor", glow);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
        }
    }

    /// <summary>Converts Blender's FBX material numbers into URP/Lit values.</summary>
    public static class ImportedSurface
    {
        /// <summary>Blender writes Shininess = (1 - roughness) * 10, so smoothness is Shininess / 10.</summary>
        public static float Smoothness(float shininess)
        {
            return Mathf.Clamp01(shininess / 10f);
        }

        public static float Metallic(float reflectionFactor)
        {
            return Mathf.Clamp01(reflectionFactor);
        }

        public static bool Glows(float r, float g, float b)
        {
            return r + g + b > 0.01f;
        }
    }
}

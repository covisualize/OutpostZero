using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Pushes URP Lit smoothness and metalness from object names so the placeholder
    /// and generated meshes read as materials before a Shader Graph bake exists.
    /// </summary>
    public class UrpMaterialPass : MonoBehaviour
    {
        private void Start()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                string name = renderer.gameObject.name;
                float metal = name.Contains("Barrel") || name.Contains("Vehicle") || name.Contains("Dumpster") ? 0.65f : 0.05f;
                float smooth = name.Contains("Ground") ? 0.08f : name.Contains("Weapon") ? 0.45f : 0.22f;
                block.SetFloat("_Metallic", metal);
                block.SetFloat("_Smoothness", smooth);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}

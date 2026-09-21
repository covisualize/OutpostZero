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
            var triplanar = Resources.Load<Material>("OutpostTriplanar");
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                string name = renderer.gameObject.name;
                if (triplanar != null && (name.Contains("Zombie") || name.Contains("Ground") || name.Contains("Barrel") || name.Contains("Road")))
                {
                    renderer.sharedMaterial = triplanar;
                }
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                float metal = name.Contains("Barrel") || name.Contains("Vehicle") || name.Contains("Dumpster") ? 0.65f : 0.05f;
                block.SetFloat("_Metallic", metal);
                block.SetColor("_BaseColor", name.Contains("Zombie") ? new Color(0.22f, 0.24f, 0.2f) : new Color(0.36f, 0.33f, 0.29f));
                block.SetColor("_RimColor", name.Contains("Zombie") ? new Color(0.75f, 0.12f, 0.08f, 1f) : new Color(0.2f, 0.18f, 0.14f, 0.4f));
                block.SetFloat("_RimPower", name.Contains("Zombie") ? 1.5f : 4f);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}

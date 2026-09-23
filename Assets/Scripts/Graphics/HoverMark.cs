using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// A thing you can use wears a thin rim. Stepping away takes the rim off.
    /// </summary>
    public static class HoverMark
    {
        public const float Width = 0.035f;
        public const float Red = 0.35f;
        public const float Green = 0.85f;
        public const float Blue = 0.95f;

        public static bool Live(bool hasTarget)
        {
            return hasTarget;
        }
    }

    public class HoverShell : MonoBehaviour
    {
        private Component shown;
        private Material ink;
        private readonly List<GameObject> hulls = new List<GameObject>();

        public void Hold(Component target)
        {
            if (target == shown) return;
            Clear();
            shown = target;
            if (!HoverMark.Live(target != null)) return;
            Paint(target);
        }

        private void OnDisable()
        {
            Clear();
        }

        private void Paint(Component target)
        {
            if (ink == null)
            {
                var shader = Shader.Find("OutpostZero/HoverHull");
                if (shader == null) return;
                ink = new Material(shader);
                ink.SetColor("_Color", new Color(HoverMark.Red, HoverMark.Green, HoverMark.Blue, 1f));
                ink.SetFloat("_Width", HoverMark.Width);
            }
            var renderers = target.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer) continue;
                if (renderer.gameObject.name == "HoverHull") continue;
                if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                {
                    var go = new GameObject("HoverHull");
                    go.transform.SetParent(skin.transform, false);
                    var copy = go.AddComponent<SkinnedMeshRenderer>();
                    copy.sharedMesh = skin.sharedMesh;
                    copy.bones = skin.bones;
                    copy.rootBone = skin.rootBone;
                    copy.sharedMaterial = ink;
                    copy.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    copy.receiveShadows = false;
                    hulls.Add(go);
                    continue;
                }
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var hull = new GameObject("HoverHull");
                hull.transform.SetParent(renderer.transform, false);
                hull.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var draw = hull.AddComponent<MeshRenderer>();
                draw.sharedMaterial = ink;
                draw.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                draw.receiveShadows = false;
                hulls.Add(hull);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < hulls.Count; i++)
            {
                if (hulls[i] != null) Destroy(hulls[i]);
            }
            hulls.Clear();
            shown = null;
        }
    }
}

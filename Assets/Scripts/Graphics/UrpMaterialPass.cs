using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Safety net after the scene loads: any renderer still on Unity's grey default material is moved
    /// onto its library family, picked from its SurfaceTag and then its name. Authored materials are
    /// never touched, so imported models keep what the FBX postprocessor gave them.
    /// </summary>
    public class UrpMaterialPass : MonoBehaviour
    {
        public int Dressed { get; private set; }

        private void Start()
        {
            Dressed = Sweep(FindObjectsByType<Renderer>(FindObjectsSortMode.None));
        }

        public static int Sweep(Renderer[] renderers)
        {
            int dressed = 0;
            if (renderers == null) return 0;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer) continue;
                if (!MaterialLibrary.IsDefault(renderer.sharedMaterial)) continue;
                var tag = renderer.GetComponentInParent<SurfaceTag>();
                var family = Pick(tag != null ? tag.Kind : SurfaceKind.Default, renderer.gameObject.name);
                if (MaterialLibrary.Dress(renderer, family)) dressed++;
            }
            return dressed;
        }

        /// <summary>The surface tag wins when it names a family; otherwise the object name decides.</summary>
        public static SurfaceFamily Pick(SurfaceKind tag, string objectName)
        {
            var family = MaterialLibrary.FromSurface(tag);
            return family != SurfaceFamily.None ? family : MaterialLibrary.Guess(objectName);
        }
    }
}

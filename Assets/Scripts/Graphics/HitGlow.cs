using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>How a struck body flashes: full at the hit, eased out over <see cref="Length"/> seconds.</summary>
    public static class HitGlowCurve
    {
        public const float Length = 0.14f;

        public static float Value(float age)
        {
            if (age < 0f || age >= Length) return 0f;
            float t = 1f - age / Length;
            return t * t;
        }
    }

    /// <summary>
    /// Drives the character shader's _HitFlash on every renderer under a body. The property block is
    /// read and written back so variety tint, rim and dissolve set by others survive.
    /// </summary>
    public class HitGlow : MonoBehaviour
    {
        private static readonly int FlashId = Shader.PropertyToID("_HitFlash");
        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float struckAt = -1f;

        public static void Strike(GameObject target)
        {
            if (target == null) return;
            var root = target.GetComponentInParent<OutpostZero.Combat.HealthSystem>();
            var host = root != null ? root.gameObject : target;
            OutpostZero.Core.Attach.Ensure<HitGlow>(host).Strike();
        }

        public void Strike()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            struckAt = Time.time;
            enabled = true;
            Apply(1f);
        }

        private void Update()
        {
            float age = Time.time - struckAt;
            float value = HitGlowCurve.Value(age);
            Apply(value);
            if (value <= 0f) enabled = false;
        }

        private void Apply(float value)
        {
            if (renderers == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(block);
                block.SetFloat(FlashId, value);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}

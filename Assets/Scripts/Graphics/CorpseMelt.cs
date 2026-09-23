using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// A dead body burns away over three seconds instead of popping out of the street.
    /// </summary>
    public class CorpseMelt : MonoBehaviour
    {
        public const float Length = 3f;

        private float started = -1f;

        public static float Amount(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= Length) return 1f;
            return age / Length;
        }

        public void Begin()
        {
            if (started < 0f) started = Time.time;
        }

        /// <summary>Stops the melt and restores the body; a pooled zombie calls this when it rises.</summary>
        public void Clear()
        {
            started = -1f;
            Apply(0f);
        }

        private void LateUpdate()
        {
            if (started < 0f) return;
            Apply(Amount(Time.time - started));
        }

        private void Apply(float amount)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var edge = new Color(1f, 0.32f, 0.06f, 1f) * amount;
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_Dissolve", amount);
                block.SetColor("_Emission", edge);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}

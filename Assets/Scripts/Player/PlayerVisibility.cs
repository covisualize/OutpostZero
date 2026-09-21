using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.Player
{
    public class PlayerVisibility : MonoBehaviour
    {
        [SerializeField] private float exposure = 0.65f;
        private PlayerController controller;

        public float Exposure => exposure;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            float value = 0.5f;
            if (controller != null)
            {
                if (controller.IsCrouching) value = 0.22f;
                else if (controller.IsSprinting) value = 0.85f;
                if (controller.FlashlightOn) value += 0.28f;
            }

            float nearest = 0f;
            for (int i = 0; i < LightSource.All.Count; i++)
            {
                var source = LightSource.All[i];
                if (source == null || source.transform.IsChildOf(transform)) continue;
                float dist = Vector3.Distance(transform.position, source.transform.position);
                if (dist < source.Radius)
                {
                    nearest = Mathf.Max(nearest, 1f - dist / source.Radius);
                }
            }

            var night = DayNightCycle.Instance;
            if (night != null && night.NightFactor > 0.45f)
            {
                value = Mathf.Lerp(value, value * 0.45f + nearest * 0.7f, night.NightFactor);
            }
            else
            {
                value += nearest * 0.15f;
            }

            exposure = Mathf.Clamp01(value);
        }
    }
}

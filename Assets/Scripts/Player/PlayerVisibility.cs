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

            float night = DayNightCycle.Instance != null ? DayNightCycle.Instance.NightFactor : 0f;
            bool crouch = controller != null && controller.IsCrouching;
            bool sprint = controller != null && controller.IsSprinting;
            bool flashlight = controller != null && controller.FlashlightOn;
            float bare = SpotRange.Exposure(crouch, sprint, flashlight, night, nearest);
            bool railLit = controller != null && controller.RailLit;
            exposure = RailLamp.Exposure(bare, railLit, flashlight);
        }
    }
}

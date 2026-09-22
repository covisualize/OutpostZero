using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// A street lamp follows the night. Noon is dark. Full night keeps the lamp's own peak.
    /// A raid that forces night lights them even if the clock says noon.
    /// </summary>
    public static class LampClock
    {
        public static float Factor(float night)
        {
            if (night <= 0f) return 0f;
            if (night >= 1f) return 1f;
            return night;
        }

        public static float Glow(float night, float peak)
        {
            if (peak < 0f) peak = 0f;
            return peak * Factor(night);
        }

        public static float Resolve(float factor, float hour)
        {
            if (factor > 0f) return Factor(factor);
            return Factor(DayNightCycle.HourToNight(hour));
        }
    }

    /// <summary>Dims a placed street lamp. Camp floodlights are not this component.</summary>
    public class DuskBulb : MonoBehaviour
    {
        private Light bulb;
        private LightSource sight;
        private float peak = 1f;
        private bool armed;

        public void Arm(float intensity)
        {
            peak = intensity < 0f ? 0f : intensity;
            armed = true;
        }

        private void Awake()
        {
            bulb = GetComponent<Light>();
            sight = GetComponent<LightSource>();
            if (!armed && bulb != null) peak = bulb.intensity;
        }

        private void LateUpdate()
        {
            if (bulb == null) bulb = GetComponent<Light>();
            float factor = DayNightCycle.Instance != null ? DayNightCycle.Instance.NightFactor : 0f;
            float hour = Colony.WorldClock.Instance != null ? Colony.WorldClock.Instance.Hour : 12f;
            float glow = LampClock.Glow(LampClock.Resolve(factor, hour), peak);
            if (bulb != null)
            {
                bulb.intensity = glow;
                bulb.enabled = glow > 0.02f;
            }
            if (sight != null) sight.enabled = glow > 0.02f;
        }
    }
}

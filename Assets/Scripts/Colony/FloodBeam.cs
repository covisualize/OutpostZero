using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A floodlight washes a circle when the generator is running. Inside that circle a crouch is no longer dark.
    /// </summary>
    public static class FloodBeam
    {
        public const float Radius = 14f;

        public static float Strength(float distance, bool powered)
        {
            if (!powered) return 0f;
            if (distance < 0f) distance = 0f;
            if (distance >= Radius) return 0f;
            return 1f - distance / Radius;
        }

        public static int Covering(string approach, float[] x, float[] z, int[] sites, int[] integrity, bool powered)
        {
            if (!powered || x == null || z == null) return 0;
            RaidPlan.AnchorOf(approach, out float anchorX, out float anchorZ);
            int count = x.Length < z.Length ? x.Length : z.Length;
            int lit = 0;
            float reach = Radius * Radius;
            for (int i = 0; i < count; i++)
            {
                if (sites != null && (i >= sites.Length || sites[i] != 0)) continue;
                if (integrity != null && (i >= integrity.Length || integrity[i] <= 0)) continue;
                float dx = x[i] - anchorX;
                float dz = z[i] - anchorZ;
                if (dx * dx + dz * dz >= reach) continue;
                lit++;
            }
            return lit;
        }

        public static int ApproachPressure(int pressure, int lamps)
        {
            if (pressure < 1) pressure = 1;
            if (lamps < 0) lamps = 0;
            if (lamps > 2) lamps = 2;
            int next = pressure - lamps * 2;
            if (next < 1) return 1;
            return next;
        }

        public static float ApproachGap(float interval, int lamps)
        {
            if (interval < 0f) interval = 0f;
            if (lamps < 0) lamps = 0;
            if (lamps > 2) lamps = 2;
            float next = interval + lamps * 0.35f;
            if (next > 3.1f) return 3.1f;
            return next;
        }
    }

    /// <summary>
    /// The sanctuary generator carries two floodlights. They shine only while it has fuel.
    /// </summary>
    public static class YardFlood
    {
        public const int Count = 2;
        public const float Height = 3.2f;
        public const float Pitch = 55f;
        public const float Yaw = 35f;
        public const float Spread = 70f;
        public const float Intensity = 3.4f;
        public static readonly Color Tint = new Color(1f, 0.93f, 0.75f);

        public static bool Lit(bool fueled)
        {
            return fueled;
        }

        public static Vector3 Local(int index)
        {
            float side = index <= 0 ? -1.6f : 1.6f;
            return new Vector3(side, Height, 0.4f);
        }

        public static Vector3 Aim(int index)
        {
            float yaw = index <= 0 ? -Yaw : Yaw;
            return new Vector3(Pitch, yaw, 0f);
        }

        public static void Raise(GameObject host)
        {
            if (host == null || host.GetComponentInChildren<YardFloodLamp>() != null) return;
            for (int i = 0; i < Count; i++)
            {
                var go = new GameObject("SanctuaryFlood");
                go.transform.SetParent(host.transform, false);
                go.transform.localPosition = Local(i);
                go.transform.localRotation = Quaternion.Euler(Aim(i));
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;
                light.range = FloodBeam.Radius;
                light.spotAngle = Spread;
                light.intensity = Intensity;
                light.color = Tint;
                light.shadows = LightShadows.Soft;
                go.AddComponent<LightSource>().Configure(FloodBeam.Radius);
                go.AddComponent<YardFloodLamp>();
            }
        }
    }

    public class YardFloodLamp : MonoBehaviour
    {
        private Light bulb;
        private LightSource sight;

        private void Awake()
        {
            bulb = GetComponent<Light>();
            sight = GetComponent<LightSource>();
        }

        private void LateUpdate()
        {
            bool on = YardFlood.Lit(CampServices.Instance != null && CampServices.Instance.GeneratorOnline);
            if (bulb != null) bulb.enabled = on;
            if (sight != null) sight.enabled = on;
        }
    }
}

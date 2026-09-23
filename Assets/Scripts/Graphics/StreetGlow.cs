using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// A sodium street lamp is warm, short, and unreliable.
    /// Three salts in every ten are dead. A live lamp dips once each flicker.
    /// </summary>
    public static class SodiumLamp
    {
        public const float Range = 9f;
        public const float Peak = 1.4f;
        public const float Dip = 0.22f;
        public const float Period = 0.35f;
        public const float Notch = 0.8f;
        public const float Width = 0.08f;
        public static readonly Color Tint = new Color(1f, 184f / 255f, 112f / 255f);

        public static int Salt(float x, float z)
        {
            int ix = Mathf.RoundToInt(x * 10f);
            int iz = Mathf.RoundToInt(z * 3f);
            int salt = ix + iz;
            if (salt < 0) salt = -salt;
            return salt;
        }

        public static bool Dead(int salt)
        {
            if (salt < 0) salt = -salt;
            return salt % 10 < 3;
        }

        public static float Flicker(float time, bool dead)
        {
            if (dead) return 0f;
            if (time < 0f) time = 0f;
            float wave = time / Period;
            wave -= Mathf.Floor(wave);
            float band = wave - Notch;
            if (band < 0f) band = -band;
            if (band >= Width) return 1f;
            float nick = 1f - band / Width;
            float glow = 1f - Dip * nick;
            if (glow < 0f) return 0f;
            return glow;
        }

        public static void Dress(GameObject host, float x, float z, float peak)
        {
            if (host == null) return;
            var light = host.GetComponent<Light>();
            if (light == null) light = host.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = Range;
            light.color = Tint;
            light.intensity = peak;
            var sight = host.GetComponent<LightSource>();
            if (sight == null) sight = host.AddComponent<LightSource>();
            sight.Configure(Range);
            var bulb = host.GetComponent<DuskBulb>();
            if (bulb == null) bulb = host.AddComponent<DuskBulb>();
            bulb.ArmSodium(Salt(x, z), peak);
        }

        public static void Raise(GameObject host)
        {
            if (host == null || host.GetComponentInChildren<DuskBulb>() != null) return;
            var lightObject = new GameObject("DistrictLamp");
            lightObject.transform.SetParent(host.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            Dress(lightObject, host.transform.position.x, host.transform.position.z, Peak);
        }
    }

    /// <summary>A live campfire breathes between a low and a high. A cold pit stays dark.</summary>
    public static class FirePulse
    {
        public const float Peak = 1.6f;
        public const float Low = 0.82f;
        public const float High = 1.18f;
        public const float Period = 0.45f;

        public static float Scale(float time, bool ready)
        {
            if (!ready) return 0f;
            if (time < 0f) time = 0f;
            float wave = Mathf.Sin(time * (Mathf.PI * 2f / Period));
            float t = (wave + 1f) * 0.5f;
            return Low + (High - Low) * t;
        }
    }

    /// <summary>A wrecked car trades its lamps. One side is on while the other is dark.</summary>
    public static class HazardBlink
    {
        public const float Period = 0.7f;
        public const float On = 0.28f;
        public const float Reach = 3.2f;

        public static bool Lit(float time, bool wreck)
        {
            if (!wreck) return false;
            if (time < 0f) time = 0f;
            float wave = time % Period;
            return wave < On;
        }

        public static bool Left(float time, bool wreck) => Lit(time, wreck);

        public static bool Right(float time, bool wreck) => wreck && !Lit(time, wreck);

        public static void Raise(GameObject host)
        {
            if (host == null || host.GetComponent<HazardLamp>() != null) return;
            host.AddComponent<HazardLamp>();
        }
    }

    public class HazardLamp : MonoBehaviour
    {
        private Light left;
        private Light right;

        private void Awake()
        {
            left = Make(-0.7f);
            right = Make(0.7f);
        }

        private Light Make(float x)
        {
            var go = new GameObject("HazardLamp");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, 0.7f, 0.4f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = HazardBlink.Reach;
            light.color = new Color(1f, 0.16f, 0.05f);
            light.intensity = 1.2f;
            go.AddComponent<LightSource>().Configure(HazardBlink.Reach);
            return light;
        }

        private void LateUpdate()
        {
            if (left != null) left.enabled = HazardBlink.Left(Time.time, true);
            if (right != null) right.enabled = HazardBlink.Right(Time.time, true);
        }
    }
}

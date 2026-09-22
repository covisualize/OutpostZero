using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [SerializeField] private Light sun;
        private Light alarm;
        public float NightFactor { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (sun == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        sun = light;
                        break;
                    }
                }
                if (sun == null)
                {
                    var go = new GameObject("Sun");
                    sun = go.AddComponent<Light>();
                    sun.type = LightType.Directional;
                    sun.shadows = LightShadows.Soft;
                }
            }

            float hour = WorldClock.Instance != null ? WorldClock.Instance.Hour : 18f;
            float angle = (hour / 24f) * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(angle, 35f, 0f);
            bool raid = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.RaidActive;
            NightFactor = raid ? 1f : HourToNight(hour);
            sun.intensity = Mathf.Lerp(1.15f, 0.08f, NightFactor);
            sun.color = Color.Lerp(new Color(1f, 0.96f, 0.9f), new Color(0.45f, 0.55f, 0.85f), NightFactor);
            RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.25f, NightFactor);
            HoldAlarm(raid);
        }

        private void HoldAlarm(bool raid)
        {
            if (alarm == null)
            {
                var go = new GameObject("RaidAlarm");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(-6f, 3.2f, -8f);
                alarm = go.AddComponent<Light>();
                alarm.type = LightType.Point;
                alarm.range = 18f;
                alarm.shadows = LightShadows.None;
                alarm.color = new Color(0.85f, 0.18f, 0.12f);
            }
            alarm.enabled = raid;
            alarm.intensity = raid ? RaidGrade.Pulse(Time.time) : 0f;
        }

        public static float HourToNight(float hour)
        {
            if (hour >= 7f && hour <= 17f) return 0f;
            if (hour > 17f && hour < 20f) return (hour - 17f) / 3f;
            if (hour >= 5f && hour < 7f) return 1f - ((hour - 5f) / 2f);
            return 1f;
        }
    }
}

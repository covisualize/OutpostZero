using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Graphics
{
    /// <summary>A slow circle over the district, high enough to read the rooftops through the fog.</summary>
    public static class MenuDrift
    {
        public const float Period = 120f;
        public const float Radius = 24f;
        public const float Height = 11f;
        public const float Bob = 0.8f;
        public const float Dusk = 0.55f;

        public static Vector3 Position(Vector3 pivot, float seconds)
        {
            float turn = Angle(seconds);
            float rise = Mathf.Sin(seconds * (2f * Mathf.PI / (Period * 0.5f))) * Bob;
            return pivot + new Vector3(Mathf.Sin(turn) * Radius, Height + rise, Mathf.Cos(turn) * Radius);
        }

        public static Vector3 Look(Vector3 pivot, float seconds)
        {
            float turn = Angle(seconds) + 0.35f;
            return pivot + new Vector3(Mathf.Sin(turn) * Radius * 0.25f, 1.5f, Mathf.Cos(turn) * Radius * 0.25f);
        }

        public static float Angle(float seconds)
        {
            float lap = seconds / Period;
            return (lap - Mathf.Floor(lap)) * 2f * Mathf.PI;
        }
    }

    /// <summary>
    /// Holds dusk and fog on the main menu and flies the camera around the street behind it.
    /// Unscaled time, because the menu runs with the game frozen.
    /// </summary>
    public class MenuBackdrop : MonoBehaviour, ISceneEntry
    {
        private bool entered;
        private bool flying;
        private Vector3 pivot;
        private float startedAt;
        private Behaviour brain;
        private bool brainWas;
        private Vector3 restorePosition;
        private Quaternion restoreRotation;
        private WeatherKind restoreWeather = WeatherKind.Clear;

        public bool Flying => flying;

        private void OnEnable() => SceneEntries.Register(this);

        private void OnDisable()
        {
            SceneEntries.Unregister(this);
            Land();
        }

        public void OnEnter(FlowContext context)
        {
            if (context.To != FlowStep.MainMenu || entered) return;
            entered = true;
            var player = PlayerRegistry.Current;
            pivot = player != null ? player.transform.position : Vector3.zero;
            startedAt = Time.unscaledTime;
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.Hold = MenuDrift.Dusk;
            if (WeatherController.Instance != null)
            {
                restoreWeather = WeatherController.Instance.Kind;
                WeatherController.Instance.SetFor(WeatherKind.Fog, 3600f);
            }
        }

        public void OnExit(FlowContext context)
        {
            if (context.From != FlowStep.MainMenu || context.To == FlowStep.MainMenu || !entered) return;
            entered = false;
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.Hold = -1f;
            if (WeatherController.Instance != null) WeatherController.Instance.SetFor(restoreWeather, 80f);
            Land();
        }

        private void LateUpdate()
        {
            bool menu = entered && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu;
            if (!menu)
            {
                Land();
                return;
            }
            if (!flying) TakeOff();
            float t = Time.unscaledTime - startedAt;
            transform.position = MenuDrift.Position(pivot, t);
            transform.rotation = Quaternion.LookRotation(MenuDrift.Look(pivot, t) - transform.position, Vector3.up);
        }

        private void TakeOff()
        {
            flying = true;
            restorePosition = transform.position;
            restoreRotation = transform.rotation;
            brain = GetComponent<Unity.Cinemachine.CinemachineBrain>();
            brainWas = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;
        }

        private void Land()
        {
            if (!flying) return;
            flying = false;
            transform.SetPositionAndRotation(restorePosition, restoreRotation);
            if (brain != null) brain.enabled = brainWas;
        }
    }
}

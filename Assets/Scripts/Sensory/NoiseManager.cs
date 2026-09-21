using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Sensory
{
    public class NoiseManager : MonoBehaviour
    {
        public static NoiseManager Instance { get; private set; }

        private readonly List<INoiseListener> listeners = new List<INoiseListener>();

        [Header("Debug Visualizer")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private float gizmoDuration = 1.2f;

        private struct ActiveNoiseGizmo
        {
            public Vector3 Origin;
            public float Radius;
            public float ExpirationTime;
            public Color Color;
        }

        private readonly List<ActiveNoiseGizmo> activeGizmos = new List<ActiveNoiseGizmo>();

        public event Action<Vector3, float, NoiseType> OnNoiseEmitted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterListener(INoiseListener listener)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        public void UnregisterListener(INoiseListener listener)
        {
            listeners.Remove(listener);
        }

        public void EmitNoise(Vector3 origin, float radius, float intensity, NoiseType noiseType, GameObject source = null)
        {
            OnNoiseEmitted?.Invoke(origin, radius, noiseType);

            if (showDebugGizmos)
            {
                Color gizmoColor = GetColorForNoise(noiseType);
                activeGizmos.Add(new ActiveNoiseGizmo
                {
                    Origin = origin,
                    Radius = radius,
                    ExpirationTime = Time.time + gizmoDuration,
                    Color = gizmoColor
                });
            }

            // Notify all registered listeners within radius
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                var listener = listeners[i];
                if (listener == null)
                {
                    listeners.RemoveAt(i);
                    continue;
                }

                float dist = Vector3.Distance(origin, listener.Position);
                float effectiveRadius = radius * listener.HearingSensitivity;

                if (dist <= effectiveRadius)
                {
                    // Check obstacle occlusion: sound attenuates through solid walls
                    float occlusionMultiplier = 1.0f;
                    Vector3 from = origin + Vector3.up * 1.2f;
                    Vector3 to = listener.Position + Vector3.up * 1.2f;
                    if (Physics.Linecast(from, to, out RaycastHit hit, GameLayers.EnvironmentMask))
                    {
                        if (hit.collider.gameObject != source && hit.collider.gameObject != (listener as Component)?.gameObject)
                        {
                            occlusionMultiplier = 0.45f;
                        }
                    }

                    float perceivedIntensity = (1.0f - (dist / effectiveRadius)) * intensity * occlusionMultiplier;
                    if (perceivedIntensity > 0.05f)
                    {
                        listener.OnHearNoise(origin, radius, perceivedIntensity, noiseType, source);
                    }
                }
            }
        }

        private Color GetColorForNoise(NoiseType noiseType)
        {
            switch (noiseType)
            {
                case NoiseType.SneakFootstep:
                    return new Color(0.2f, 0.8f, 0.2f, 0.4f);
                case NoiseType.WalkFootstep:
                    return new Color(0.9f, 0.9f, 0.2f, 0.5f);
                case NoiseType.SprintFootstep:
                    return new Color(1f, 0.6f, 0f, 0.6f);
                case NoiseType.MeleeSwing:
                    return new Color(0.3f, 0.5f, 1f, 0.5f);
                case NoiseType.GunshotQuiet:
                    return new Color(1f, 0.4f, 0f, 0.7f);
                case NoiseType.GunshotLoud:
                case NoiseType.Explosion:
                    return new Color(1f, 0.1f, 0.1f, 0.85f);
                default:
                    return new Color(1f, 1f, 1f, 0.5f);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            float now = Time.time;
            for (int i = activeGizmos.Count - 1; i >= 0; i--)
            {
                var gizmo = activeGizmos[i];
                if (now > gizmo.ExpirationTime)
                {
                    activeGizmos.RemoveAt(i);
                    continue;
                }

                Gizmos.color = gizmo.Color;
                Gizmos.DrawWireSphere(gizmo.Origin, gizmo.Radius);
            }
        }
    }
}

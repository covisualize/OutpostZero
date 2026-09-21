using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Sensory
{
    public class NoiseEmitter : MonoBehaviour
    {
        [Header("Default Emission Settings")]
        [SerializeField] private float defaultRadius = 10f;
        [SerializeField] private float defaultIntensity = 1f;
        [SerializeField] private NoiseType defaultType = NoiseType.WalkFootstep;

        public void Emit()
        {
            EmitCustom(defaultRadius, defaultIntensity, defaultType);
        }

        public void EmitCustom(float radius, float intensity, NoiseType type)
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.EmitNoise(transform.position, radius, intensity, type, gameObject);
            }
        }
    }
}

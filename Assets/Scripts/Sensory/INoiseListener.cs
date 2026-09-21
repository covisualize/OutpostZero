using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Sensory
{
    public interface INoiseListener
    {
        Vector3 Position { get; }
        float HearingSensitivity { get; }
        void OnHearNoise(Vector3 origin, float radius, float intensity, NoiseType noiseType, GameObject source);
    }
}

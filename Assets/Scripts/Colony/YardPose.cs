using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>
    /// How a camp mate reads while they work. Rest squats. A guard leans back.
    /// A cook, a builder, and a hauler swing. Everyone else stands up.
    /// </summary>
    public static class YardPose
    {
        public const float RestScale = 0.72f;
        public const float RestLean = 22f;
        public const float GuardLean = -8f;
        public const float CookLean = 14f;
        public const float BuildLean = 28f;
        public const float MedicLean = 16f;
        public const float ClearLean = 18f;
        public const float ScavengeLean = 10f;

        public static float Stir(float age)
        {
            if (age < 0f) age = 0f;
            float t = age % 0.8f;
            if (t < 0.4f) return t / 0.4f;
            return 1f - (t - 0.4f) / 0.4f;
        }

        public static float Lean(string action, float age)
        {
            if (action == "Visit") return 6f;
            if (action == "Rest") return RestLean;
            if (action == "Guard") return GuardLean;
            if (action == "Cook") return CookLean * Stir(age);
            if (action == "Build") return BuildLean * Stir(age);
            if (action == "Medic") return MedicLean;
            if (action == "Clear") return ClearLean * Stir(age);
            if (action == "Scavenge") return ScavengeLean;
            return 0f;
        }

        public static float Scale(string action)
        {
            if (action == "Rest") return RestScale;
            return 1f;
        }
    }

    /// <summary>Keeps the swing clock on the camp capsule.</summary>
    public class YardBeat : MonoBehaviour
    {
        public float Age;

        private void Update()
        {
            Age += Time.deltaTime;
        }
    }
}

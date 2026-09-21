using UnityEngine;

namespace OutpostZero.Combat
{
    public class WeaponMod : MonoBehaviour
    {
        public float damageMultiplier = 1f;
        public float noiseMultiplier = 1f;
        public float spreadMultiplier = 1f;
        public string modId = "none";

        public void ApplySuppressor()
        {
            modId = "suppressor";
            damageMultiplier = 0.9f;
            noiseMultiplier = 0.4f;
            spreadMultiplier = 0.85f;
        }
    }
}

using UnityEngine;

namespace OutpostZero.Combat
{
    public class WeaponMod : MonoBehaviour
    {
        public float damageMultiplier = 1f;
        public float noiseMultiplier = 1f;
        public float spreadMultiplier = 1f;
        public int magazineBonus;
        public string modId = "none";
        private bool suppressor;
        private bool optic;
        private bool extendedMag;

        public void ApplySuppressor() => Apply("suppressor");

        public void Apply(string id)
        {
            if (id == "suppressor") suppressor = true;
            else if (id == "optic") optic = true;
            else if (id == "extended_mag") extendedMag = true;
            damageMultiplier = suppressor ? 0.9f : 1f;
            noiseMultiplier = suppressor ? 0.4f : 1f;
            spreadMultiplier = (suppressor ? 0.85f : 1f) * (optic ? 0.55f : 1f);
            magazineBonus = extendedMag ? 10 : 0;
            modId = id;
        }

        public static Profile ProfileFor(string id)
        {
            switch (id)
            {
                case "suppressor": return new Profile("suppressor", 0.9f, 0.4f, 0.85f, 0);
                case "optic": return new Profile("optic", 1f, 1f, 0.55f, 0);
                case "extended_mag": return new Profile("extended_mag", 1f, 1f, 1f, 10);
                default: return new Profile("none", 1f, 1f, 1f, 0);
            }
        }

        public struct Profile
        {
            public string id;
            public float damage;
            public float noise;
            public float spread;
            public int magazineBonus;

            public Profile(string id, float damage, float noise, float spread, int magazineBonus)
            {
                this.id = id;
                this.damage = damage;
                this.noise = noise;
                this.spread = spread;
                this.magazineBonus = magazineBonus;
            }
        }
    }
}

using System;
using UnityEngine;
using OutpostZero.Core;

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
        private bool rail;

        public bool HasSuppressor => suppressor;
        public bool HasRail => rail;

        public static NoiseType Report(NoiseType kind, bool suppressed)
        {
            if (!suppressed) return kind;
            if (kind == NoiseType.GunshotLoud || kind == NoiseType.GunshotQuiet) return NoiseType.GunshotQuiet;
            return kind;
        }

        public void ApplySuppressor() => Apply("suppressor");

        public void Apply(string id)
        {
            if (id == "suppressor") suppressor = true;
            else if (id == "optic") optic = true;
            else if (id == "extended_mag") extendedMag = true;
            else if (id == "rail") rail = true;
            Recalculate();
        }

        public string Pack() => PackFlags(suppressor, optic, extendedMag, rail);

        public void Restore(string slot)
        {
            ReadFlags(slot, out suppressor, out optic, out extendedMag);
            rail = RailOn(slot);
            Recalculate();
        }

        public static string PackFlags(bool hasSuppressor, bool hasOptic, bool hasExtendedMag, bool hasRail = false)
        {
            string packed = "";
            if (hasSuppressor) packed = Append(packed, "suppressor");
            if (hasOptic) packed = Append(packed, "optic");
            if (hasExtendedMag) packed = Append(packed, "extended_mag");
            if (hasRail) packed = Append(packed, "rail");
            return packed;
        }

        public static bool RailOn(string slot)
        {
            if (string.IsNullOrEmpty(slot)) return false;
            string[] parts = slot.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "rail") return true;
            }
            return false;
        }

        public static void ReadFlags(string slot, out bool hasSuppressor, out bool hasOptic, out bool hasExtendedMag)
        {
            hasSuppressor = false;
            hasOptic = false;
            hasExtendedMag = false;
            if (string.IsNullOrEmpty(slot)) return;
            string[] parts = slot.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "suppressor") hasSuppressor = true;
                else if (parts[i] == "optic") hasOptic = true;
                else if (parts[i] == "extended_mag") hasExtendedMag = true;
            }
        }

        public static string JoinSlots(string[] slots)
        {
            if (slots == null || slots.Length == 0) return "";
            return string.Join("|", slots);
        }

        public static string[] SplitSlots(string packed)
        {
            if (string.IsNullOrEmpty(packed)) return Array.Empty<string>();
            return packed.Split('|');
        }

        public static Profile Combine(string slot)
        {
            ReadFlags(slot, out bool hasSuppressor, out bool hasOptic, out bool hasExtendedMag);
            return new Profile(
                string.IsNullOrEmpty(slot) ? "none" : slot,
                hasSuppressor ? 0.9f : 1f,
                hasSuppressor ? 0.4f : 1f,
                (hasSuppressor ? 0.85f : 1f) * (hasOptic ? 0.55f : 1f),
                hasExtendedMag ? 10 : 0);
        }

        private void Recalculate()
        {
            damageMultiplier = suppressor ? 0.9f : 1f;
            noiseMultiplier = suppressor ? 0.4f : 1f;
            spreadMultiplier = (suppressor ? 0.85f : 1f) * (optic ? 0.55f : 1f);
            magazineBonus = extendedMag ? 10 : 0;
            string packed = Pack();
            modId = string.IsNullOrEmpty(packed) ? "none" : packed;
        }

        private static string Append(string packed, string id)
        {
            return string.IsNullOrEmpty(packed) ? id : packed + "+" + id;
        }

        public static Profile ProfileFor(string id)
        {
            switch (id)
            {
                case "suppressor": return new Profile("suppressor", 0.9f, 0.4f, 0.85f, 0);
                case "optic": return new Profile("optic", 1f, 1f, 0.55f, 0);
                case "extended_mag": return new Profile("extended_mag", 1f, 1f, 1f, 10);
                case "rail": return new Profile("rail", 1f, 1f, 1f, 0);
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

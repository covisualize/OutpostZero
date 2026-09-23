using OutpostZero.Core;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Every sound the game plays. A pistol, a shotgun, a rifle, and a burst gun
    /// each have a tone, and so does each step. An unknown id is missing.
    /// </summary>
    public static class ClipBook
    {
        public static readonly string[] Ids =
        {
            "ambient", "pulse", "rain", "storm", "wind", "ash",
            "step", "step_hard", "step_metal", "step_wood", "step_water", "step_gravel", "step_glass",
            "ui", "stem_perc", "stem_combat",
            "stinger_kill", "stinger_death", "stinger_extract", "stinger_raid", "stinger_dawn",
            "gun", "shotgun", "rifle", "smg", "swing", "gun_far", "boom", "boom_far", "kill",
            "thunder", "heart", "breath", "dry", "mag_out", "mag_in", "rack",
            "groan", "shriek", "roar", "snarl", "stomp", "grunt",
            "hum", "crackle", "buzz", "flies", "hiss", "chop", "whoosh", "clang", "pained",
            "take_soft", "take_box", "take_metal", "clink", "clack",
            "spark", "splinter", "dust", "spray", "mist", "splash", "burn", "cloud", "spit", "scream", "drip", "creak", "bite", "cough"
        };

        public static string Fire(WeaponType type)
        {
            if (type == WeaponType.Shotgun) return "shotgun";
            if (type == WeaponType.Rifle) return "rifle";
            if (type == WeaponType.SMG) return "smg";
            if (type == WeaponType.Melee) return "swing";
            return "gun";
        }

        /// <summary>A weapon's own fire sound when it names a known clip, else the one for its type.</summary>
        public static string Fire(WeaponType type, string authored)
        {
            return type != WeaponType.Melee && Has(authored) ? authored : Fire(type);
        }

        /// <summary>A surface's own footstep when it names a known step clip, else the step for its kind or collider name.</summary>
        public static string Step(string authored, SurfaceKind kind, string colliderName)
        {
            if (Has(authored) && authored.StartsWith("step", System.StringComparison.Ordinal)) return authored;
            return kind != SurfaceKind.Default ? SurfaceTag.StepId(kind) : AudioMix.StepId(colliderName);
        }

        public static bool Has(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < Ids.Length; i++)
            {
                if (Ids[i] == id) return true;
            }
            return false;
        }

        public static float Mark(string id)
        {
            return Tone(id, 0.25f, 1f);
        }

        public static float Tone(string id, float t, float noise)
        {
            if (id == "scream") return Mathf.Sin(t * 90f);
            if (id == "pulse") return Mathf.Sin(t * 28f);
            if (id == "rain") return noise;
            if (id == "storm") return noise * Mathf.Sin(t * 4.4f);
            if (id == "wind") return noise * Mathf.Sin(t * 6f);
            if (id == "ash") return noise * Mathf.Sin(t * 2.2f);
            if (id == "step") return noise * Mathf.Sin(t * 18f);
            if (id == "step_hard") return noise * Mathf.Sin(t * 33f);
            if (id == "step_metal") return Mathf.Sin(t * 88f);
            if (id == "step_wood") return noise * Mathf.Sin(t * 13f);
            if (id == "step_water") return noise * Mathf.Sin(t * 6.5f);
            if (id == "step_gravel") return noise * Mathf.Sin(t * 25f);
            if (id == "step_glass") return noise * Mathf.Sin(t * 64f);
            if (id == "ui") return Mathf.Sin(t * 40f);
            if (id == "stem_perc") return Mathf.Sin(t * 48f) > 0.65f ? noise : 0f;
            if (id == "stem_combat") return Mathf.Sin(t * 16f);
            if (id == "stinger_kill") return Mathf.Sin(t * 55f);
            if (id == "stinger_death") return Mathf.Sin(t * 8f);
            if (id == "stinger_extract") return Mathf.Sin(t * 32f);
            if (id == "stinger_raid") return noise * Mathf.Sin(t * 12f);
            if (id == "stinger_dawn") return Mathf.Sin(t * 22f);
            if (id == "gun") return Mathf.Sin(t * 150f);
            if (id == "shotgun") return noise * Mathf.Sin(t * 10.5f);
            if (id == "rifle") return Mathf.Sin(t * 96f);
            if (id == "smg") return Mathf.Sin(t * 210f);
            if (id == "swing") return noise * Mathf.Sin(t * 27f);
            if (id == "kill") return Mathf.Sin(t * 20f);
            if (id == "boom") return noise * Mathf.Sin(t * 5.5f);
            if (id == "ambient") return Mathf.Sin(t * 2.4f);
            if (id == "gun_far") return Mathf.Sin(t * 9f);
            if (id == "boom_far") return noise * Mathf.Sin(t * 4f);
            if (id == "thunder") return noise * Mathf.Sin(t * 3f);
            if (id == "heart") return Mathf.Sin(t * 7f);
            if (id == "breath") return noise * Mathf.Sin(t * 3f);
            if (id == "dry") return Mathf.Sin(t * 90f);
            if (id == "mag_out" || id == "mag_in") return noise * Mathf.Sin(t * 14f);
            if (id == "rack") return Mathf.Sin(t * 28f);
            if (id == "groan") return noise * Mathf.Sin(t * 5f);
            if (id == "shriek") return Mathf.Sin(t * 74f);
            if (id == "roar") return noise * Mathf.Sin(t * 3.5f);
            if (id == "snarl") return noise * Mathf.Sin(t * 36f);
            if (id == "stomp") return noise * Mathf.Sin(t * 2.5f);
            if (id == "grunt") return Mathf.Sin(t * 18f);
            if (id == "hum") return Mathf.Sin(t * 6f);
            if (id == "crackle") return noise;
            if (id == "buzz") return Mathf.Sin(t * 55f) * 0.35f;
            if (id == "flies") return noise * Mathf.Sin(t * 90f) * 0.4f;
            if (id == "hiss") return noise * Mathf.Sin(t * 28f);
            if (id == "chop") return noise * Mathf.Sin(t * 12f);
            if (id == "whoosh") return noise * Mathf.Sin(t * 22f);
            if (id == "clang") return Mathf.Sin(t * 70f);
            if (id == "pained") return Mathf.Sin(t * 16f);
            if (id == "take_soft") return Mathf.Sin(t * 24f);
            if (id == "take_box") return noise * Mathf.Sin(t * 18f);
            if (id == "take_metal") return Mathf.Sin(t * 40f);
            if (id == "clink") return Mathf.Sin(t * 120f);
            if (id == "clack") return noise * Mathf.Sin(t * 40f);
            if (id == "spark") return Mathf.Sin(t * 140f);
            if (id == "splinter") return noise * Mathf.Sin(t * 22f);
            if (id == "dust") return noise;
            if (id == "spray") return noise * Mathf.Sin(t * 18f);
            if (id == "mist") return noise * Mathf.Sin(t * 8f);
            if (id == "splash") return noise * Mathf.Sin(t * 30f);
            if (id == "burn") return noise * Mathf.Sin(t * 11f);
            if (id == "cloud") return noise * Mathf.Sin(t * 4f);
            if (id == "spit") return Mathf.Sin(t * 160f);
            if (id == "drip") return Mathf.Sin(t * 48f);
            if (id == "creak") return noise * Mathf.Sin(t * 7f);
            if (id == "bite") return noise * Mathf.Sin(t * 18f);
            if (id == "cough") return noise * Mathf.Sin(t * 14f);
            return noise;
        }
    }
}

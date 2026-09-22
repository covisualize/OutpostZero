using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Core
{
    public enum SurfaceKind
    {
        Default = 0,
        Concrete = 1,
        Metal = 2,
        Wood = 3,
        Glass = 4,
        Gravel = 5,
        Water = 6,
        Flesh = 7
    }

    /// <summary>
    /// What a prefab is made of, for footsteps and impacts. Generated prefabs get one from their
    /// sidecar's material names (BlenderScripts/prefab_tags.py keeps the same keyword table).
    /// </summary>
    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceKind kind;

        public SurfaceKind Kind => kind;

        public void Set(SurfaceKind value)
        {
            kind = value;
        }

        private static readonly SurfaceKind[] Order = { SurfaceKind.Concrete, SurfaceKind.Metal, SurfaceKind.Wood, SurfaceKind.Glass, SurfaceKind.Gravel };

        private static readonly Dictionary<SurfaceKind, string[]> Words = new Dictionary<SurfaceKind, string[]>
        {
            { SurfaceKind.Concrete, new[] { "concrete", "asphalt", "road", "sidewalk", "curb", "brick", "plaster", "foundation", "tile", "bldg", "ruin", "stone", "rubble" } },
            { SurfaceKind.Metal, new[] { "metal", "steel", "iron", "rust", "gunmetal", "corrugated", "tin", "brass", "car", "truck", "dumpster", "barrel", "ibeam", "gen" } },
            { SurfaceKind.Wood, new[] { "wood", "timber", "plank", "crate", "bench", "log" } },
            { SurfaceKind.Glass, new[] { "glass" } },
            { SurfaceKind.Gravel, new[] { "sand", "gravel", "dirt", "soil" } },
        };

        public static SurfaceKind CategoryDefault(string category)
        {
            switch (category)
            {
                case "Characters": return SurfaceKind.Flesh;
                case "BaseBuilding": return SurfaceKind.Wood;
                case "Props":
                case "Weapons": return SurfaceKind.Metal;
                case "Kit":
                case "Environment": return SurfaceKind.Concrete;
                default: return SurfaceKind.Default;
            }
        }

        public static SurfaceKind Guess(string category, IEnumerable<string> materials)
        {
            var fallback = CategoryDefault(category);
            if (fallback == SurfaceKind.Flesh || materials == null) return fallback;
            var votes = new Dictionary<SurfaceKind, int>();
            foreach (var material in materials)
            {
                if (string.IsNullOrEmpty(material)) continue;
                foreach (var token in material.ToLowerInvariant().Split('_'))
                {
                    if (token.Length == 0 || token == "mat") continue;
                    foreach (var kind in Order)
                    {
                        if (!Matches(token, Words[kind])) continue;
                        votes.TryGetValue(kind, out int count);
                        votes[kind] = count + 1;
                        break;
                    }
                }
            }
            int best = 0;
            foreach (var pair in votes) best = Mathf.Max(best, pair.Value);
            if (best == 0) return fallback;
            foreach (var kind in Order)
            {
                if (votes.TryGetValue(kind, out int count) && count == best) return kind;
            }
            return fallback;
        }

        public static string StepId(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Concrete: return "step_hard";
                case SurfaceKind.Metal: return "step_metal";
                case SurfaceKind.Wood: return "step_wood";
                case SurfaceKind.Glass: return "step_glass";
                case SurfaceKind.Gravel: return "step_gravel";
                case SurfaceKind.Water: return "step_water";
                default: return "step";
            }
        }

        public static SurfaceTag Of(Collider collider)
        {
            return collider != null ? collider.GetComponentInParent<SurfaceTag>() : null;
        }

        private static bool Matches(string token, string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (token.Contains(words[i])) return true;
            }
            return false;
        }
    }
}

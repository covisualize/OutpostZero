using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>The PBR surface families in Assets/Materials/Library (written by material_library.py).</summary>
    public enum SurfaceFamily
    {
        None = 0,
        Asphalt = 1,
        ConcreteCracked = 2,
        BrickRed = 3,
        BrickGrey = 4,
        MetalRusted = 5,
        MetalPainted = 6,
        Plywood = 7,
        TarpFabric = 8,
        Glass = 9,
        Rubber = 10,
        RotFlesh = 11,
        Cloth = 12,
        ChainLink = 13,
    }

    /// <summary>
    /// Runtime handle on the material library: each family has a URP/Lit material (ML_*, UV mapped)
    /// and, except glass and chain-link, a world-space triplanar one (MT_*) for boxes and primitives with no UVs.
    /// Also owns the naming convention that maps Blender materials (Mat_Family_Variant) and
    /// primitive names onto families, so nothing in the scene keeps Unity's grey default.
    /// </summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Material Library", fileName = "MaterialLibrary")]
    public class MaterialLibrary : ScriptableObject
    {
        public const string ResourcePath = "MaterialLibrary";
        public const string Folder = "Assets/Materials/Library";

        [Serializable]
        public class Entry
        {
            public SurfaceFamily family;
            public Material lit;
            public Material triplanar;
        }

        public Entry[] entries = new Entry[0];

        private static MaterialLibrary loaded;
        private static bool tried;
        private static MaterialPropertyBlock block;

        public static MaterialLibrary Active
        {
            get
            {
                if (!tried)
                {
                    tried = true;
                    loaded = Resources.Load<MaterialLibrary>(ResourcePath);
                }
                return loaded;
            }
        }

        public Entry Find(SurfaceFamily family)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
                if (entry != null && entry.family == family) return entry;
            return null;
        }

        public static Material Lit(SurfaceFamily family)
        {
            var entry = Active != null ? Active.Find(family) : null;
            return entry != null ? entry.lit : null;
        }

        /// <summary>Glass and chain-link carry alpha, so they only come UV mapped.</summary>
        public static bool HasTriplanar(SurfaceFamily family)
        {
            return family != SurfaceFamily.None && family != SurfaceFamily.Glass && family != SurfaceFamily.ChainLink;
        }

        /// <summary>The world-space material for a family; glass and chain-link give their lit one.</summary>
        public static Material Triplanar(SurfaceFamily family)
        {
            var entry = Active != null ? Active.Find(family) : null;
            if (entry == null) return null;
            return entry.triplanar != null ? entry.triplanar : entry.lit;
        }

        public static string LitPath(SurfaceFamily family)
        {
            return Folder + "/ML_" + family + ".mat";
        }

        public static string TriplanarPath(SurfaceFamily family)
        {
            return Folder + "/MT_" + family + ".mat";
        }

        /// <summary>
        /// Puts a primitive on its family's triplanar material. The tint rides a property block on
        /// _Tint so the shared material stays untouched. False when the library is missing.
        /// </summary>
        public static bool Dress(Renderer renderer, SurfaceFamily family, Color tint)
        {
            if (renderer == null || family == SurfaceFamily.None) return false;
            var material = Triplanar(family);
            if (material == null) return false;
            renderer.sharedMaterial = material;
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (Plain(tint))
            {
                block.Clear();
            }
            else
            {
                block.SetColor("_Tint", tint);
                block.SetColor("_BaseColor", tint);
            }
            renderer.SetPropertyBlock(block);
            return true;
        }

        public static bool Dress(Renderer renderer, SurfaceFamily family)
        {
            return Dress(renderer, family, Color.white);
        }

        /// <summary>Dresses a primitive by its name, keeping <paramref name="tint"/> as a colour cast.</summary>
        public static bool DressByName(Renderer renderer, Color tint)
        {
            if (renderer == null) return false;
            return Dress(renderer, Guess(renderer.gameObject.name), TintFor(tint));
        }

        /// <summary>
        /// A flat prototype colour becomes a gentle cast over the texture: mid greys leave the texture
        /// alone and saturated colours keep their hue, so painted crates still read as painted.
        /// </summary>
        public static Color TintFor(Color flat)
        {
            float max = Mathf.Max(flat.r, Mathf.Max(flat.g, flat.b));
            float min = Mathf.Min(flat.r, Mathf.Min(flat.g, flat.b));
            if (max - min < 0.08f) return Color.white;
            float scale = max > 0.001f ? 1f / max : 1f;
            return new Color(
                Mathf.Lerp(1f, flat.r * scale, 0.6f),
                Mathf.Lerp(1f, flat.g * scale, 0.6f),
                Mathf.Lerp(1f, flat.b * scale, 0.6f),
                1f);
        }

        private static bool Plain(Color tint)
        {
            return Mathf.Abs(tint.r - 1f) < 0.001f && Mathf.Abs(tint.g - 1f) < 0.001f && Mathf.Abs(tint.b - 1f) < 0.001f;
        }

        /// <summary>True for Unity's built-in grey material (what CreatePrimitive assigns) or none.</summary>
        public static bool IsDefault(Material material)
        {
            if (material == null) return true;
            return IsDefaultName(material.name);
        }

        public static bool IsDefaultName(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            string clean = name.Replace(" (Instance)", "");
            return clean == "Default-Material" || clean == "Lit" || clean == "Default-Diffuse" || clean == "Default Material";
        }

        public static SurfaceFamily FromSurface(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Concrete: return SurfaceFamily.ConcreteCracked;
                case SurfaceKind.Metal: return SurfaceFamily.MetalPainted;
                case SurfaceKind.Wood: return SurfaceFamily.Plywood;
                case SurfaceKind.Glass: return SurfaceFamily.Glass;
                case SurfaceKind.Gravel: return SurfaceFamily.Asphalt;
                case SurfaceKind.Flesh: return SurfaceFamily.RotFlesh;
                default: return SurfaceFamily.None;
            }
        }

        private static readonly string[][] NameRules =
        {
            new[] { "Glass", "glass", "bulb", "window", "pane" },
            new[] { "Asphalt", "asphalt", "road", "street", "ground", "slab", "tile", "sidewalk" },
            new[] { "BrickGrey", "ruin", "rubble" },
            new[] { "BrickRed", "brick", "bldg", "building", "wall" },
            new[] { "RotFlesh", "zombie", "flesh", "mutant", "veins", "blood", "meat", "bone", "corpse" },
            new[] { "MetalRusted", "rust", "rusted", "rebar", "scrap", "corrugated", "barrel", "dumpster", "wreck" },
            new[] { "TarpFabric", "tarp", "canvas", "sandbag", "tent", "blanket", "backpack", "pack", "awning", "stall" },
            new[] { "Cloth", "cloth", "shirt", "pants", "jacket", "coat", "hoodie", "overalls", "dungarees", "denim", "scarf", "flannel", "camo", "vest", "gauze", "rag", "pillow", "twine", "leather", "cot", "bed" },
            new[] { "Rubber", "rubber", "tire", "tyre", "pad", "polymer", "lid", "cord", "hose" },
            new[] { "Plywood", "wood", "timber", "lumber", "pallet", "bracing", "stock", "pegboard", "crate", "logs", "stakes", "plank", "shelf", "desk", "table", "counter", "door", "board", "barricade" },
            new[] { "ConcreteCracked", "concrete", "curb", "plaster", "foundation", "stones", "floor", "trim", "jersey", "pillar", "block", "room", "roof" },
            new[] { "ChainLink", "chainlink", "chain", "mesh" },
            new[] { "MetalPainted", "metal", "steel", "iron", "ironwork", "chrome", "gunmetal", "tin", "ibeam", "beam", "frame", "latch", "latches", "clasp", "clasps", "buckle", "buckles", "silver", "panel", "engine", "tube", "mag", "spigot", "wire", "barbed", "pipe", "blade", "guard", "car", "truck", "sedan", "vehicle", "post", "pole", "fence", "gun", "rifle", "shotgun", "rollup", "generator", "locker" },
        };

        private static readonly HashSet<string> Unmapped = new HashSet<string>
        {
            "eye", "face", "thumb", "skin", "label", "stencil", "stripe", "stripes", "cross", "ink", "band", "hazard", "biohazard", "sign", "signboard", "foil", "card", "embers", "fire",
        };

        /// <summary>
        /// Family for a Blender material named by the Mat_Family_Variant convention (or any object
        /// name). Eyes, faces, skin, printed labels and hazard stripes stay unmapped so their authored
        /// colour survives; the importer falls back to the Principled values for those.
        /// </summary>
        public static SurfaceFamily FamilyFor(string materialName)
        {
            var tokens = Tokens(materialName);
            if (tokens.Count == 0) return SurfaceFamily.None;
            foreach (var token in tokens)
                if (Unmapped.Contains(token)) return SurfaceFamily.None;
            foreach (var rule in NameRules)
            {
                for (int i = 1; i < rule.Length; i++)
                {
                    if (tokens.Contains(rule[i])) return (SurfaceFamily)Enum.Parse(typeof(SurfaceFamily), rule[0]);
                }
            }
            return SurfaceFamily.None;
        }

        /// <summary>Family for a runtime primitive by its object name; unknown pieces read as cracked concrete.</summary>
        public static SurfaceFamily Guess(string objectName)
        {
            var family = FamilyFor(objectName);
            return family != SurfaceFamily.None ? family : SurfaceFamily.ConcreteCracked;
        }

        /// <summary>Lower-case words of a name: "Mat_Car_TireRubber" gives car, tire, rubber.</summary>
        public static List<string> Tokens(string name)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(name)) return tokens;
            var word = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool split = c == '_' || c == ' ' || c == '-' || c == '.' || c == '(' || c == ')' || char.IsDigit(c);
                bool upper = char.IsUpper(c) && word.Length > 0 && !char.IsUpper(name[i - 1]);
                if (split || upper)
                {
                    Flush(word, tokens);
                    if (split) continue;
                }
                word.Append(char.ToLowerInvariant(c));
            }
            Flush(word, tokens);
            tokens.Remove("mat");
            return tokens;
        }

        private static void Flush(System.Text.StringBuilder word, List<string> tokens)
        {
            if (word.Length == 0) return;
            tokens.Add(word.ToString());
            word.Length = 0;
        }
    }

    /// <summary>Rim colour by faction so bodies read from the top-down camera.</summary>
    public static class SurfaceRim
    {
        public static readonly Color Survivor = new Color(0.3f, 0.85f, 1f, 0.5f);
        public static readonly Color Zombie = new Color(0.55f, 0.85f, 0.25f, 0.6f);
        public static readonly Color Prop = new Color(0.22f, 0.2f, 0.16f, 0.12f);

        public static Color For(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return Prop;
            if (modelName.StartsWith("Zombie_", StringComparison.Ordinal)) return Zombie;
            if (modelName.StartsWith("Survivor_", StringComparison.Ordinal)
                || modelName.StartsWith("Colonist_", StringComparison.Ordinal)
                || modelName.StartsWith("NPC_", StringComparison.Ordinal)) return Survivor;
            return Prop;
        }
    }
}

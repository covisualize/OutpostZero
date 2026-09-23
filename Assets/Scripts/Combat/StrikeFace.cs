using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A hit reads the thing it met: flesh sprays, metal sparks, wood splinters, stone dusts.
    /// </summary>
    public static class StrikeFace
    {
        public const float Spray = 0.4f;
        public const float Spark = 0.38f;
        public const float Splinter = 0.36f;
        public const float Dust = 0.28f;

        public static string Of(GameObject target)
        {
            if (target == null) return "concrete";
            if (target.GetComponentInParent<ZombieAI>() != null) return "flesh";
            var tag = target.GetComponentInParent<SurfaceTag>();
            if (tag != null)
            {
                string tagged = OfSurface(tag.Kind);
                if (tagged != null) return tagged;
            }
            Transform cursor = target.transform;
            while (cursor != null)
            {
                string kind = OfName(cursor.name);
                if (kind != "concrete") return kind;
                cursor = cursor.parent;
            }
            return "concrete";
        }

        /// <summary>The impact family for a tagged prefab; null leaves it to the name.</summary>
        public static string OfSurface(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Metal: return "metal";
                case SurfaceKind.Wood: return "wood";
                case SurfaceKind.Flesh: return "flesh";
                case SurfaceKind.Concrete:
                case SurfaceKind.Gravel:
                case SurfaceKind.Glass:
                    return "concrete";
                default: return null;
            }
        }

        public static string OfName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "concrete";
            if (Has(name, "Zombie")) return "flesh";
            if (Has(name, "Barrel") || Has(name, "Dumpster") || Has(name, "Vehicle") || Has(name, "Sedan") || Has(name, "Truck") || Has(name, "Lamp") || Has(name, "Turret"))
                return "metal";
            if (Has(name, "Crate") || Has(name, "Wood") || Has(name, "Door") || Has(name, "Board") || Has(name, "Barricade"))
                return "wood";
            return "concrete";
        }

        public static string Sound(string kind)
        {
            if (kind == "flesh") return "spray";
            if (kind == "metal") return "spark";
            if (kind == "wood") return "splinter";
            return "dust";
        }

        public static float Volume(string kind)
        {
            if (kind == "flesh") return Spray;
            if (kind == "metal") return Spark;
            if (kind == "wood") return Splinter;
            return Dust;
        }

        private static bool Has(string name, string token)
        {
            return name.IndexOf(token, System.StringComparison.Ordinal) >= 0;
        }
    }
}

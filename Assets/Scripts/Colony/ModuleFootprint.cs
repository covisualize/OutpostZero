using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The ground a module claims: its table size rounded up to whole metres, centred on the snapped anchor and
    /// turned with the facing. Placement refuses a footprint that overlaps another or leaves the fence.
    /// </summary>
    public static class ModuleFootprint
    {
        public const float Unit = 1f;
        private const float Slack = 0.001f;

        public struct Area
        {
            public float MinX;
            public float MinZ;
            public float MaxX;
            public float MaxZ;
        }

        public static void Cells(string kind, out int wide, out int deep)
        {
            Vector3 size = GridBuilder.Scale(kind, 100);
            wide = Mathf.Max(1, Mathf.CeilToInt(size.x / Unit - Slack));
            deep = Mathf.Max(1, Mathf.CeilToInt(size.z / Unit - Slack));
        }

        /// <summary>Cells along world x and z once turned; a quarter turn swaps them.</summary>
        public static void Span(string kind, int rotation, out int alongX, out int alongZ)
        {
            Cells(kind, out int wide, out int deep);
            bool sideways = Quarter(rotation) % 2 == 1;
            alongX = sideways ? deep : wide;
            alongZ = sideways ? wide : deep;
        }

        public static Area Of(string kind, int rotation, float x, float z)
        {
            Span(kind, rotation, out int alongX, out int alongZ);
            return Box(x, z, alongX * Unit, alongZ * Unit);
        }

        public static Area Box(float x, float z, float sizeX, float sizeZ)
        {
            return new Area { MinX = x - sizeX * 0.5f, MaxX = x + sizeX * 0.5f, MinZ = z - sizeZ * 0.5f, MaxZ = z + sizeZ * 0.5f };
        }

        /// <summary>Touching edges share a line, not ground, so a wall run of barricades fits end to end.</summary>
        public static bool Overlaps(Area a, Area b)
        {
            return a.MinX < b.MaxX - Slack && b.MinX < a.MaxX - Slack && a.MinZ < b.MaxZ - Slack && b.MinZ < a.MaxZ - Slack;
        }

        public static bool Holds(Area area, float x, float z)
        {
            return x >= area.MinX && x <= area.MaxX && z >= area.MinZ && z <= area.MaxZ;
        }

        public static bool Clashes(IReadOnlyList<PlacedModule> placed, Area area)
        {
            if (placed == null) return false;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module == null) continue;
                if (Overlaps(area, Of(module.kind, module.rotation, module.x, module.z))) return true;
            }
            return false;
        }

        public static bool Clashes(IReadOnlyList<PlacedModule> placed, string kind, int rotation, float x, float z)
        {
            return Clashes(placed, Of(kind, rotation, x, z));
        }

        public static bool Inside(Area area)
        {
            return MapRim.Inside(area.MinX, area.MinZ) && MapRim.Inside(area.MaxX, area.MinZ)
                && MapRim.Inside(area.MinX, area.MaxZ) && MapRim.Inside(area.MaxX, area.MaxZ);
        }

        private static int Quarter(int rotation)
        {
            int turns = Mathf.RoundToInt(rotation / 90f) % 4;
            return turns < 0 ? turns + 4 : turns;
        }
    }
}

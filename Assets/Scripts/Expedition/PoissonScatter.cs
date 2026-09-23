using System;
using System.Collections.Generic;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Bridson Poisson-disk sampling in a rectangle, thinned by a density mask. Every random draw is a
    /// hash of (seed, counter), so a seed always gives the same points on every machine.
    /// </summary>
    public static class PoissonScatter
    {
        public const int Tries = 24;

        public struct Point
        {
            public float X;
            public float Z;
        }

        /// <summary>
        /// Points at least <paramref name="spacing"/> apart inside [minX, maxX] x [minZ, maxZ].
        /// <paramref name="keep"/> filters the domain (the street) and <paramref name="density"/>
        /// gives the chance, 0 to 1, that a well-spaced point survives there.
        /// </summary>
        public static List<Point> Sample(int seed, float minX, float maxX, float minZ, float maxZ, float spacing,
            Func<float, float, bool> keep, Func<float, float, float> density, int limit = 4096)
        {
            var kept = new List<Point>();
            if (spacing <= 0f || maxX <= minX || maxZ <= minZ) return kept;
            float cell = spacing / 1.41421356f;
            int cols = Math.Max(1, (int)Math.Ceiling((maxX - minX) / cell));
            int rows = Math.Max(1, (int)Math.Ceiling((maxZ - minZ) / cell));
            var grid = new int[cols * rows];
            for (int i = 0; i < grid.Length; i++) grid[i] = -1;
            var all = new List<Point>();
            var active = new List<int>();
            int draw = 0;

            var first = new Point { X = Lerp(minX, maxX, Unit(seed, draw++)), Z = Lerp(minZ, maxZ, Unit(seed, draw++)) };
            Place(first);
            while (active.Count > 0 && all.Count < limit)
            {
                int pick = (int)(Unit(seed, draw++) * active.Count);
                if (pick >= active.Count) pick = active.Count - 1;
                var from = all[active[pick]];
                bool found = false;
                for (int t = 0; t < Tries; t++)
                {
                    float angle = Unit(seed, draw++) * 6.2831853f;
                    float radius = spacing * (1f + Unit(seed, draw++));
                    var next = new Point { X = from.X + (float)Math.Cos(angle) * radius, Z = from.Z + (float)Math.Sin(angle) * radius };
                    if (next.X < minX || next.X > maxX || next.Z < minZ || next.Z > maxZ) continue;
                    if (!Clear(next)) continue;
                    Place(next);
                    found = true;
                    break;
                }
                if (!found) active.RemoveAt(pick);
            }

            for (int i = 0; i < all.Count; i++)
            {
                var point = all[i];
                if (keep != null && !keep(point.X, point.Z)) continue;
                float chance = density != null ? density(point.X, point.Z) : 1f;
                if (Unit(seed + 7919, i) < chance) kept.Add(point);
            }
            return kept;

            void Place(Point point)
            {
                all.Add(point);
                active.Add(all.Count - 1);
                grid[Index(point)] = all.Count - 1;
            }

            int Index(Point point)
            {
                int cx = Clamp((int)((point.X - minX) / cell), 0, cols - 1);
                int cz = Clamp((int)((point.Z - minZ) / cell), 0, rows - 1);
                return cz * cols + cx;
            }

            bool Clear(Point point)
            {
                int cx = Clamp((int)((point.X - minX) / cell), 0, cols - 1);
                int cz = Clamp((int)((point.Z - minZ) / cell), 0, rows - 1);
                float min = spacing * spacing;
                for (int z = Math.Max(0, cz - 2); z <= Math.Min(rows - 1, cz + 2); z++)
                {
                    for (int x = Math.Max(0, cx - 2); x <= Math.Min(cols - 1, cx + 2); x++)
                    {
                        int index = grid[z * cols + x];
                        if (index < 0) continue;
                        float dx = all[index].X - point.X;
                        float dz = all[index].Z - point.Z;
                        if (dx * dx + dz * dz < min) return false;
                    }
                }
                return true;
            }
        }

        public static float Unit(int seed, int index)
        {
            uint x = unchecked((uint)seed * 747796405u + (uint)index * 2891336453u + 12345u);
            x ^= x >> 16;
            x = unchecked(x * 2246822519u);
            x ^= x >> 13;
            x = unchecked(x * 3266489917u);
            x ^= x >> 16;
            return (x & 0xFFFFFFu) / 16777216f;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }

    /// <summary>
    /// Where litter gathers: thin in the open road, thick against walls and in the gutter along curbs.
    /// </summary>
    public struct DebrisMask
    {
        public float Base;
        public float Edge;
        public float Reach;
        public float[] Anchors;

        public static DebrisMask Street(float[] anchors, float baseDensity = 0.12f, float edgeDensity = 0.9f, float reach = 1.6f)
        {
            return new DebrisMask { Base = baseDensity, Edge = edgeDensity, Reach = reach, Anchors = anchors };
        }

        public float At(float x, float z)
        {
            float nearest = Nearest(x, z);
            if (float.IsPositiveInfinity(nearest)) return Base;
            float t = nearest / Math.Max(0.01f, Reach);
            float pull = (float)Math.Exp(-t * t);
            return Base + (Edge - Base) * pull;
        }

        public float Nearest(float x, float z)
        {
            float best = float.PositiveInfinity;
            if (Anchors == null) return best;
            for (int i = 0; i + 1 < Anchors.Length; i += 2)
            {
                float dx = Anchors[i] - x;
                float dz = Anchors[i + 1] - z;
                float d = dx * dx + dz * dz;
                if (d < best) best = d;
            }
            return float.IsPositiveInfinity(best) ? best : (float)Math.Sqrt(best);
        }
    }
}

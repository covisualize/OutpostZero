using System.Collections.Generic;
using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// East neighborhood for a district: a road grid with alleys, lot shells, and a few
    /// cells removed. The approach and the avenue stay in place, so the spawn can always
    /// walk to the far gate. The same seed and district rebuild the same streets.
    /// </summary>
    public static class RoadGraph
    {
        public const float Step = 4f;
        public const float CurbHeight = 0.15f;
        public const float LaneClear = 1.6f;
        public const float CurbBreadth = 0.18f;
        public const float SidewalkBreadth = 0.8f;

        public struct Cell
        {
            public float X;
            public float Z;
            public string Kind;
        }

        public struct Edge
        {
            public float X;
            public float Z;
            public float Yaw;
            public string Kind;
        }

        public struct Map
        {
            public string Id;
            public int Seed;
            public string Footprint;
            public Cell[] Cells;
            public float PoiX;
            public float PoiZ;
            public float ExtractX;
            public float ExtractZ;
            public float LootX;
            public float LootZ;
            public bool HasLoot;
            public float NestX;
            public float NestZ;
            public bool HasNest;
        }

        public static Map Build(int seed, string districtId)
        {
            seed = DistrictGenerator.Resolve(seed);
            if (string.IsNullOrEmpty(districtId)) districtId = "ash_market";
            var cells = new List<Cell>();
            for (float x = 4f; x <= 20.01f; x += Step) cells.Add(At(x, 0f, "spine"));
            for (int col = 0; col < 5; col++)
            {
                float x = 24f + col * Step;
                cells.Add(At(x, 0f, col == 4 ? "extract" : "spine"));
            }
            for (int row = 1; row < 4; row++)
            {
                cells.Add(At(32f, row * Step, row == 3 ? "poi" : "road"));
            }

            var map = new Map
            {
                Id = districtId,
                Seed = seed,
                Footprint = DistrictBlocks.Footprint(districtId),
                PoiX = 32f,
                PoiZ = 12f,
                ExtractX = 40f,
                ExtractZ = 0f
            };

            bool authored = districtId == "ash_market" || districtId == "old_hospital" || districtId == "downtown_core";
            int[] cols = { 0, 1, 3, 4 };
            for (int row = 1; row < 4; row++)
            {
                for (int i = 0; i < cols.Length; i++)
                {
                    int col = cols[i];
                    float x = 24f + col * Step;
                    float z = row * Step;
                    string kind = authored
                        ? Landmark(districtId, col, row)
                        : Rolled(seed, districtId, col, row, cells, x, z);
                    cells.Add(At(x, z, kind));
                    if (kind == "lot" && !map.HasLoot)
                    {
                        map.HasLoot = true;
                        map.LootX = x;
                        map.LootZ = z;
                    }
                    if (kind == "hole" && !map.HasNest)
                    {
                        map.HasNest = true;
                        map.NestX = x;
                        map.NestZ = z;
                    }
                }
            }

            map.Cells = cells.ToArray();
            return map;
        }

        public static bool Navigable(Map map)
        {
            var cells = map.Cells;
            if (cells == null || cells.Length == 0) return false;
            int start = -1;
            float best = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                if (!Open(cells[i].Kind)) continue;
                float score = cells[i].X * cells[i].X + cells[i].Z * cells[i].Z;
                if (start < 0 || score < best)
                {
                    start = i;
                    best = score;
                }
            }
            if (start < 0) return false;

            var seen = new bool[cells.Length];
            var queue = new int[cells.Length];
            int head = 0;
            int tail = 0;
            seen[start] = true;
            queue[tail++] = start;
            bool poi = false;
            bool extract = false;
            while (head < tail)
            {
                int i = queue[head++];
                if (cells[i].Kind == "poi") poi = true;
                if (cells[i].Kind == "extract") extract = true;
                for (int j = 0; j < cells.Length; j++)
                {
                    if (seen[j] || !Open(cells[j].Kind)) continue;
                    float dx = cells[i].X - cells[j].X;
                    float dz = cells[i].Z - cells[j].Z;
                    if (dx * dx + dz * dz <= 16.1f)
                    {
                        seen[j] = true;
                        queue[tail++] = j;
                    }
                }
            }
            return poi && extract;
        }

        public static string KindAt(Map map, float x, float z)
        {
            var cells = map.Cells;
            if (cells == null) return "";
            for (int i = 0; i < cells.Length; i++)
            {
                if (Close(cells[i].X, x) && Close(cells[i].Z, z)) return cells[i].Kind;
            }
            return "";
        }

        public static Edge[] Edges(Map map)
        {
            var list = new List<Edge>();
            var cells = map.Cells;
            if (cells == null) return new Edge[0];
            for (int i = 0; i < cells.Length; i++)
            {
                if (!Open(cells[i].Kind)) continue;
                float x = cells[i].X;
                float z = cells[i].Z;
                bool east = OpenAt(cells, x + Step, z);
                bool west = OpenAt(cells, x - Step, z);
                bool north = OpenAt(cells, x, z + Step);
                bool south = OpenAt(cells, x, z - Step);
                int links = (east ? 1 : 0) + (west ? 1 : 0) + (north ? 1 : 0) + (south ? 1 : 0);
                if (!north) Side(list, x, z, 0f, 1f);
                if (!south) Side(list, x, z, 0f, -1f);
                if (!east) Side(list, x, z, 1f, 0f);
                if (!west && !(x <= 4.01f && Close(z, 0f))) Side(list, x, z, -1f, 0f);
                if (links >= 3) list.Add(new Edge { X = x, Z = z, Kind = "cross" });
                else if (east && west && !north && !south) list.Add(new Edge { X = x, Z = z, Kind = "dash" });
                else if (north && south && !east && !west) list.Add(new Edge { X = x, Z = z, Yaw = 90f, Kind = "dash" });
            }
            return list.ToArray();
        }

        public static bool ClearsBuildings(float x, float z)
        {
            if (x > 10.4f && x < 20.5f && z > 1f && z < 11.3f) return false;
            return true;
        }

        public static string Signature(Map map)
        {
            if (map.Cells == null) return "";
            var builder = new StringBuilder();
            for (int i = 0; i < map.Cells.Length; i++)
            {
                if (i > 0) builder.Append(';');
                builder.Append(map.Cells[i].Kind);
            }
            return builder.ToString();
        }

        private static string Landmark(string id, int col, int row)
        {
            if (id == "ash_market")
            {
                if (row == 1) return "alley";
                if (row == 2) return "lot";
                return "hole";
            }
            if (id == "old_hospital")
            {
                if (col == 0) return "alley";
                if (col == 1 || col == 3) return "lot";
                return "hole";
            }
            if (row == 2) return "road";
            if (row == 1 && col == 4) return "hole";
            if (row == 1) return "lot";
            if (row == 3 && col == 0) return "hole";
            return "lot";
        }

        private static string Rolled(int seed, string id, int col, int row, List<Cell> cells, float x, float z)
        {
            uint roll = Mix(seed, id, col * 8 + row);
            int pick = (int)(roll % 5u);
            if (pick == 0 || !Touches(cells, x, z)) return "hole";
            if (pick == 1) return "alley";
            return "lot";
        }

        private static bool Touches(List<Cell> cells, float x, float z)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!Open(cells[i].Kind)) continue;
                float dx = cells[i].X - x;
                float dz = cells[i].Z - z;
                if (dx < 0f) dx = -dx;
                if (dz < 0f) dz = -dz;
                if (dx + dz <= Step + 0.05f && dx + dz > 0.05f) return true;
            }
            return false;
        }

        private static void Side(List<Edge> list, float x, float z, float nx, float nz)
        {
            float half = LaneClear * 0.5f;
            float curbAlong = nx != 0f ? 90f : 0f;
            float curbX = x + nx * (half + CurbBreadth * 0.5f);
            float curbZ = z + nz * (half + CurbBreadth * 0.5f);
            if (ClearsBuildings(curbX, curbZ)) list.Add(new Edge { X = curbX, Z = curbZ, Yaw = curbAlong, Kind = "curb" });
            float walkX = x + nx * (half + CurbBreadth + SidewalkBreadth * 0.5f);
            float walkZ = z + nz * (half + CurbBreadth + SidewalkBreadth * 0.5f);
            if (ClearsBuildings(walkX, walkZ)) list.Add(new Edge { X = walkX, Z = walkZ, Yaw = curbAlong, Kind = "walk" });
        }

        private static bool OpenAt(Cell[] cells, float x, float z)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (!Open(cells[i].Kind)) continue;
                if (Close(cells[i].X, x) && Close(cells[i].Z, z)) return true;
            }
            return false;
        }

        private static bool Open(string kind)
        {
            return kind == "spine" || kind == "road" || kind == "alley" || kind == "poi" || kind == "extract";
        }

        private static Cell At(float x, float z, string kind)
        {
            return new Cell { X = x, Z = z, Kind = kind };
        }

        private static bool Close(float a, float b)
        {
            float d = a - b;
            if (d < 0f) d = -d;
            return d < 0.2f;
        }

        private static uint Mix(int seed, string id, int index)
        {
            uint hash = 2166136261u;
            unchecked
            {
                hash ^= (uint)seed;
                hash *= 16777619u;
                if (id != null)
                {
                    for (int i = 0; i < id.Length; i++)
                    {
                        hash ^= id[i];
                        hash *= 16777619u;
                    }
                }
                hash ^= (uint)index;
                hash *= 16777619u;
            }
            return hash;
        }
    }
}

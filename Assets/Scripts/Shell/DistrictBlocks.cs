using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>
    /// A west-side block for a district: a lane between two lot rows, a point of interest,
    /// and an extraction site. The lane stays off the prop street, so the same seed
    /// rebuilds a walk from the spawn to both the point of interest and the way out.
    /// </summary>
    public static class DistrictBlocks
    {
        public struct Cell
        {
            public float X;
            public float Z;
            public string Kind;
        }

        public struct Plan
        {
            public string Id;
            public int Seed;
            public string Footprint;
            public string PoiRole;
            public string ExtractKind;
            public float PoiX;
            public float PoiZ;
            public float ExtractX;
            public float ExtractZ;
            public float NestX;
            public float NestZ;
        }

        public static Plan Build(int seed, string districtId)
        {
            seed = DistrictGenerator.Resolve(seed);
            if (string.IsNullOrEmpty(districtId)) districtId = "ash_market";
            uint roll = Mix(seed, districtId, 3);
            var plan = new Plan
            {
                Id = districtId,
                Seed = seed,
                Footprint = Footprint(districtId),
                PoiRole = PoiRole(districtId),
                PoiX = -10f,
                NestX = -14f
            };

            if (districtId == "ash_market")
            {
                plan.ExtractKind = "gate";
                plan.PoiZ = 4f;
            }
            else if (districtId == "old_hospital")
            {
                plan.ExtractKind = "alley";
                plan.PoiZ = 8f;
            }
            else if (districtId == "downtown_core")
            {
                plan.ExtractKind = "plaza";
                plan.PoiZ = 14f;
            }
            else
            {
                plan.ExtractKind = CampaignBoard.Tier(districtId) >= 3 ? "plaza" : "alley";
                plan.PoiZ = 4f + (roll % 6u) * 2f;
            }

            if (plan.ExtractKind == "gate")
            {
                plan.ExtractX = -5.5f;
                plan.ExtractZ = -10f;
            }
            else if (plan.ExtractKind == "plaza")
            {
                plan.ExtractX = -12f;
                plan.ExtractZ = 16f;
            }
            else
            {
                plan.ExtractX = -14f;
                plan.ExtractZ = 0f;
            }

            float nestZ = 6f + ((roll >> 4) % 5u) * 2f;
            if (Close(nestZ, plan.PoiZ)) nestZ = nestZ >= 14f ? 6f : nestZ + 2f;
            if (plan.ExtractKind == "alley" && Close(nestZ, 0f)) nestZ = 6f;
            plan.NestZ = nestZ;
            return plan;
        }

        public static Cell[] Open(Plan plan)
        {
            var cells = new List<Cell>();
            cells.Add(At(0f, 0f, "road"));
            for (float x = -2f; x >= -12.01f; x -= 2f)
            {
                cells.Add(At(x, 0f, Close(x, -10f) ? "door" : "road"));
            }
            for (float z = 2f; z <= 16.01f; z += 2f)
            {
                cells.Add(At(-12f, z, Close(z, plan.PoiZ) ? "door" : "road"));
            }
            cells.Add(At(plan.PoiX, plan.PoiZ, "poi"));
            cells.Add(At(plan.NestX, plan.NestZ, "nest"));
            if (plan.ExtractKind == "gate")
            {
                cells.Add(At(0f, -2f, "road"));
                cells.Add(At(0f, -4f, "road"));
                cells.Add(At(0f, -6f, "road"));
                cells.Add(At(0f, -8f, "road"));
                cells.Add(At(0f, -10f, "road"));
                cells.Add(At(-2f, -10f, "road"));
                cells.Add(At(-4f, -10f, "road"));
            }
            Stamp(cells, plan.ExtractX, plan.ExtractZ, "extract");
            return cells.ToArray();
        }

        public static Cell[] Walls(Plan plan)
        {
            var cells = new List<Cell>();
            for (float z = 2f; z <= 16.01f; z += 2f)
            {
                if (!Close(z, plan.PoiZ)) cells.Add(At(-10f, z, "wall"));
            }
            for (float z = 0f; z <= 16.01f; z += 2f)
            {
                if (Close(z, plan.NestZ)) continue;
                if (plan.ExtractKind == "alley" && Close(z, 0f)) continue;
                cells.Add(At(-14f, z, "wall"));
            }
            return cells.ToArray();
        }

        public static bool Navigable(Plan plan)
        {
            var open = Open(plan);
            if (open.Length == 0) return false;
            int start = IndexNear(open, 0f, 0f);
            if (start < 0) return false;
            var seen = new bool[open.Length];
            var queue = new int[open.Length];
            int head = 0;
            int tail = 0;
            seen[start] = true;
            queue[tail++] = start;
            bool poi = false;
            bool extract = false;
            while (head < tail)
            {
                int i = queue[head++];
                if (Close(open[i].X, plan.PoiX) && Close(open[i].Z, plan.PoiZ)) poi = true;
                if (Close(open[i].X, plan.ExtractX) && Close(open[i].Z, plan.ExtractZ)) extract = true;
                for (int j = 0; j < open.Length; j++)
                {
                    if (seen[j]) continue;
                    float dx = open[i].X - open[j].X;
                    float dz = open[i].Z - open[j].Z;
                    if (dx * dx + dz * dz <= 4.04f)
                    {
                        seen[j] = true;
                        queue[tail++] = j;
                    }
                }
            }
            return poi && extract;
        }

        public static bool ClearOf(Cell[] cells, DistrictLayout.Piece[] pieces, float gap)
        {
            if (cells == null || pieces == null) return true;
            float min = gap * gap;
            for (int i = 0; i < cells.Length; i++)
            {
                for (int p = 0; p < pieces.Length; p++)
                {
                    float dx = cells[i].X - pieces[p].X;
                    float dz = cells[i].Z - pieces[p].Z;
                    if (dx * dx + dz * dz < min) return false;
                }
            }
            return true;
        }

        public static string Footprint(string id)
        {
            if (id == "old_hospital") return "clinic";
            if (id == "downtown_core") return "station";
            if (id == "rail_yard" || id == "water_plant") return "warehouse";
            if (id == "police_station") return "hospital";
            if (id == "north_gate" || id == "highway_overpass") return "apartment";
            return "storefront";
        }

        public static string PoiRole(string id)
        {
            if (id == "old_hospital" || id == "police_station" || id == "downtown_core") return "radio";
            return "cache";
        }

        private static Cell At(float x, float z, string kind)
        {
            return new Cell { X = x, Z = z, Kind = kind };
        }

        private static void Stamp(List<Cell> cells, float x, float z, string kind)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!Close(cells[i].X, x) || !Close(cells[i].Z, z)) continue;
                var cell = cells[i];
                cell.Kind = kind;
                cells[i] = cell;
                return;
            }
            cells.Add(At(x, z, kind));
        }

        private static bool Close(float a, float b)
        {
            float d = a - b;
            if (d < 0f) d = -d;
            return d < 0.2f;
        }

        private static int IndexNear(Cell[] cells, float x, float z)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                if (Close(cells[i].X, x) && Close(cells[i].Z, z)) return i;
            }
            return -1;
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

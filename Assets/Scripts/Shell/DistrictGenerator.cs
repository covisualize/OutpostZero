using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Seeded props for a district. The same seed and id always rebuild the same street,
    /// and the center lane from the spawn stays empty.
    /// </summary>
    public static class DistrictGenerator
    {
        public const int DefaultSeed = 1701;

        public static int Resolve(int seed)
        {
            return seed == 0 ? DefaultSeed : seed;
        }

        public static DistrictLayout.Piece[] Scatter(int seed, string districtId)
        {
            seed = Resolve(seed);
            if (string.IsNullOrEmpty(districtId)) districtId = "ash_market";
            int count = 8 + (int)(Mix(seed, districtId, 0) % 5u);
            var placed = new List<DistrictLayout.Piece>();
            for (int i = 0; i < count; i++)
            {
                if (TryPlace(seed, districtId, i, placed, out var piece)) placed.Add(piece);
            }
            return placed.ToArray();
        }

        public static bool LaneClear(DistrictLayout.Piece piece)
        {
            float x = piece.X < 0f ? -piece.X : piece.X;
            return x >= 1.6f;
        }

        public static bool Spaced(DistrictLayout.Piece[] pieces, float gap)
        {
            if (pieces == null) return true;
            float min = gap * gap;
            for (int i = 0; i < pieces.Length; i++)
            {
                for (int j = i + 1; j < pieces.Length; j++)
                {
                    float dx = pieces[i].X - pieces[j].X;
                    float dz = pieces[i].Z - pieces[j].Z;
                    if (dx * dx + dz * dz < min) return false;
                }
            }
            return true;
        }

        private static bool TryPlace(int seed, string districtId, int index, List<DistrictLayout.Piece> placed, out DistrictLayout.Piece piece)
        {
            piece = default;
            for (int salt = 0; salt < 8; salt++)
            {
                uint roll = Mix(seed, districtId, index * 8 + salt + 1);
                float unitA = (roll & 65535u) / 65535f;
                float unitB = ((roll >> 8) & 65535u) / 65535f;
                bool left = (roll & 1u) == 0u;
                float x = left ? -4.2f + unitA * 2.4f : 1.8f + unitA * 5.2f;
                float z = 3.2f + unitB * 14.2f;
                var candidate = new DistrictLayout.Piece
                {
                    Role = Role(districtId, roll >> 16),
                    X = x,
                    Z = z,
                    Yaw = (roll % 4u) * 15f
                };
                if (!DistrictLayout.StaysOnTheStreet(candidate) || !LaneClear(candidate)) continue;
                if (!Apart(candidate, placed, 1.05f)) continue;
                piece = candidate;
                return true;
            }
            return false;
        }

        private static bool Apart(DistrictLayout.Piece piece, List<DistrictLayout.Piece> placed, float gap)
        {
            float min = gap * gap;
            for (int i = 0; i < placed.Count; i++)
            {
                float dx = piece.X - placed[i].X;
                float dz = piece.Z - placed[i].Z;
                if (dx * dx + dz * dz < min) return false;
            }
            return true;
        }

        private static string Role(string id, uint roll)
        {
            int n = (int)(roll % 5u);
            bool medical = id == "old_hospital" || id == "police_station";
            bool military = id == "rail_yard" || id == "north_gate" || id == "highway_overpass" || id == "downtown_core" || id == "water_plant";
            if (medical)
            {
                if (n == 0 || n == 4) return "crate_medical";
                if (n == 1) return "barrel_toxic";
                if (n == 2) return "cover";
                return "lamp";
            }
            if (military)
            {
                if (n == 0) return "crate_military";
                if (n == 1) return "barrel_explosive";
                if (n == 2) return "cover";
                if (n == 3) return "barrel_oil";
                return "lamp";
            }
            if (n == 0 || n == 4) return "stall";
            if (n == 1) return "crate";
            if (n == 2) return "lamp";
            return "cover";
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
                hash ^= hash >> 16;
                hash *= 0x85ebca6bu;
                hash ^= hash >> 13;
                hash *= 0xc2b2ae35u;
                hash ^= hash >> 16;
            }
            return hash;
        }
    }
}

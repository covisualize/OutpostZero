using System.Collections.Generic;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Deterministic street debris, sanctuary clutter, and the skyline past the fence.
    /// </summary>
    public static class DressingPlan
    {
        public struct Mark
        {
            public string Role;
            public float X;
            public float Y;
            public float Z;
            public float Yaw;
            public float W;
            public float H;
            public float D;
        }

        private static readonly string[] DebrisRoles = { "rubble", "paper", "bottle", "tyre", "brick", "glass" };

        public static int SeedFor(string id)
        {
            int seed = 17;
            if (string.IsNullOrEmpty(id)) return seed;
            for (int i = 0; i < id.Length; i++) seed = unchecked(seed * 31 + id[i]);
            return seed;
        }

        public static Mark[] Debris(string districtId)
        {
            return Scatter(SeedFor(districtId), 18, -4f, 16f, 2f, 16f, 0.85f);
        }

        public static Mark[] Patches(string districtId)
        {
            int seed = SeedFor(districtId) + 91;
            var marks = new List<Mark>();
            int attempt = 0;
            while (marks.Count < 5 && attempt < 40)
            {
                float x = Lerp(-5f, 6f, Unit(seed, attempt++));
                float z = Lerp(3f, 15f, Unit(seed, attempt++));
                if (!OnTheStreet(x, z)) continue;
                marks.Add(Box("patch", x, 0.02f, z, 0f, 1.6f, 0.04f, 1.15f));
            }
            return marks.ToArray();
        }

        public static Mark[] Horizon()
        {
            return new[]
            {
                Box("skyline", -12f, 0f, 28f, 0f, 3.2f, 8f, 0.5f),
                Box("skyline", -4f, 0f, 28.5f, 0f, 2.4f, 12f, 0.5f),
                Box("skyline", 5f, 0f, 28f, 0f, 2.8f, 7f, 0.5f),
                Box("skyline", 13f, 0f, 29f, 0f, 3.4f, 14f, 0.5f),
                Box("tower", 18f, 0f, 25f, 0f, 2.2f, 8f, 2.2f),
                Box("pole", -6f, 0f, 16f, 0f, 0.22f, 4.6f, 0.22f),
                Box("pole", 8f, 0f, 16.4f, 0f, 0.22f, 4.6f, 0.22f),
                Box("overpass", -9f, 2.6f, 21.2f, 8f, 6f, 0.55f, 2.4f),
                Box("overpass", 11f, 2.2f, 21.4f, -12f, 5.5f, 0.7f, 2.6f)
            };
        }

        public static Mark[] Home()
        {
            return new[]
            {
                Box("tent", -22f, 0f, -9f, 20f, 2.4f, 1.5f, 1.8f),
                Box("tent", -22f, 0f, -15f, -15f, 2.2f, 1.4f, 1.7f),
                Box("tarp", -20.2f, 0.12f, -12.2f, 10f, 2.4f, 0.06f, 1.6f),
                Box("laundry_a", -21f, 0f, -18f, 0f, 0.12f, 2.3f, 0.12f),
                Box("laundry_b", -8f, 0f, -18f, 0f, 0.12f, 2.3f, 0.12f),
                Box("sandbag", -22f, 0f, -6f, 0f, 1.1f, 0.45f, 0.45f),
                Box("sandbag", -22f, 0f, -4f, 0f, 1.1f, 0.45f, 0.45f),
                Box("sandbag", -20f, 0f, -3.2f, 25f, 1.1f, 0.45f, 0.45f),
                Box("cable", -16.6f, 0.08f, -9.2f, 30f, 1.1f, 0.05f, 0.08f),
                Box("cable", -15.2f, 0.06f, -7.6f, 40f, 1.1f, 0.05f, 0.08f),
                Box("bulb", -18f, 2.05f, -18f, 0f, 0.14f, 0.14f, 0.14f),
                Box("bulb", -14f, 2.05f, -18f, 0f, 0.14f, 0.14f, 0.14f),
                Box("bulb", -10f, 2.05f, -18f, 0f, 0.14f, 0.14f, 0.14f)
            };
        }

        public static bool OnTheStreet(float x, float z)
        {
            if (x * x + z * z <= 9f) return false;
            if (x <= -6f && z <= -4f) return false;
            return z > 1f && z < 20f;
        }

        public static bool ClearsHome(float x, float z)
        {
            float reach = 1.4f * 1.4f;
            for (int i = 0; i < HomeAnchors.Length; i += 2)
            {
                float dx = x - HomeAnchors[i];
                float dz = z - HomeAnchors[i + 1];
                if (dx * dx + dz * dz < reach) return false;
            }
            return true;
        }

        public static bool Spaced(Mark[] marks, float spacing)
        {
            if (marks == null) return true;
            float min = spacing * spacing;
            for (int i = 0; i < marks.Length; i++)
            {
                for (int j = i + 1; j < marks.Length; j++)
                {
                    float dx = marks[i].X - marks[j].X;
                    float dz = marks[i].Z - marks[j].Z;
                    if (dx * dx + dz * dz < min) return false;
                }
            }
            return true;
        }

        private static readonly float[] HomeAnchors =
        {
            -8f, -8f,
            -5.5f, -10f,
            -16f, -16f,
            -12f, -12f,
            -14f, -8.5f,
            -18f, -11f,
            -10f, -15f,
            -14.5f, -14f,
            -14f, -9.5f,
            -9.5f, -10f,
            -11f, -14.5f,
            -13.5f, -8.5f
        };

        private static Mark[] Scatter(int seed, int count, float minX, float maxX, float minZ, float maxZ, float spacing)
        {
            var marks = new List<Mark>();
            int attempt = 0;
            int guard = 0;
            while (marks.Count < count && guard < count * 30)
            {
                guard++;
                float x = Lerp(minX, maxX, Unit(seed, attempt++));
                float z = Lerp(minZ, maxZ, Unit(seed, attempt++));
                if (!OnTheStreet(x, z)) continue;
                if (!FarFrom(marks, x, z, spacing)) continue;
                string role = DebrisRoles[UnitIndex(seed, attempt++) % DebrisRoles.Length];
                marks.Add(Sized(role, x, z, Unit(seed, attempt++) * 360f));
            }
            return marks.ToArray();
        }

        private static bool FarFrom(List<Mark> marks, float x, float z, float spacing)
        {
            float min = spacing * spacing;
            for (int i = 0; i < marks.Count; i++)
            {
                float dx = marks[i].X - x;
                float dz = marks[i].Z - z;
                if (dx * dx + dz * dz < min) return false;
            }
            return true;
        }

        private static Mark Sized(string role, float x, float z, float yaw)
        {
            switch (role)
            {
                case "paper": return Box(role, x, 0.02f, z, yaw, 0.28f, 0.02f, 0.2f);
                case "bottle": return Box(role, x, 0f, z, yaw, 0.08f, 0.22f, 0.08f);
                case "tyre": return Box(role, x, 0.16f, z, yaw, 0.42f, 0.16f, 0.42f);
                case "brick": return Box(role, x, 0.06f, z, yaw, 0.22f, 0.08f, 0.12f);
                case "glass": return Box(role, x, 0.03f, z, yaw, 0.12f, 0.04f, 0.1f);
                default: return Box(role, x, 0.08f, z, yaw, 0.34f, 0.16f, 0.28f);
            }
        }

        private static Mark Box(string role, float x, float y, float z, float yaw, float w, float h, float d)
        {
            return new Mark { Role = role, X = x, Y = y, Z = z, Yaw = yaw, W = w, H = h, D = d };
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        private static float Unit(int seed, int index)
        {
            uint mixed = Mix(seed, index);
            return (mixed & 0xFFFFFFu) / 16777215f;
        }

        private static int UnitIndex(int seed, int index) => (int)(Mix(seed, index) & 0x7FFFFFFFu);

        private static uint Mix(int seed, int index)
        {
            uint x = unchecked((uint)seed * 747796405u + (uint)index * 2891336453u);
            x = (x ^ (x >> 16)) * 2246822519u;
            return x;
        }
    }
}

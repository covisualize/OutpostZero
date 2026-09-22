namespace OutpostZero.Expedition
{
    /// <summary>
    /// The street door opens into a front room, then a second room to the east.
    /// The shared wall keeps a two-meter gap, and the furniture stays off that lane.
    /// </summary>
    public static class RoomPlan
    {
        public const float Span = 8f;
        public const float Wall = 3.6f;
        public const float Gap = 2f;

        public struct Piece
        {
            public string Name;
            public float X;
            public float Y;
            public float Z;
            public float W;
            public float H;
            public float D;
            public bool Solid;
        }

        public static void Annex(float x, float z, out float annexX, out float annexZ)
        {
            annexX = x + Span;
            annexZ = z;
        }

        public static int Opening(float x, float z, Piece[] into)
        {
            float depth = (7.4f - Gap) * 0.5f;
            float shift = Gap * 0.5f + depth * 0.5f;
            into[0] = Box("RoomWall", x + Wall, 1.4f, z - shift, 0.3f, 2.8f, depth, true);
            into[1] = Box("RoomWall", x + Wall, 1.4f, z + shift, 0.3f, 2.8f, depth, true);
            return 2;
        }

        public static int Dress(string footprint, float x, float z, Piece[] into)
        {
            Annex(x, z, out float ax, out float az);
            int n = 0;
            into[n++] = Box("RoomFloor", ax, -0.1f, az, Span, 0.2f, Span, true);
            into[n++] = Box("RoomWall", ax, 1.4f, az - Wall, Span, 2.8f, 0.3f, true);
            into[n++] = Box("RoomWall", ax, 1.4f, az + Wall, Span, 2.8f, 0.3f, true);
            into[n++] = Box("RoomWall", ax + Wall, 1.4f, az, 0.3f, 2.8f, 7.4f, true);
            into[n++] = Furnish(footprint, ax, az);
            return n;
        }

        public static bool Covers(Piece piece, float x, float z)
        {
            if (!piece.Solid) return false;
            float dx = x - piece.X;
            if (dx < 0f) dx = -dx;
            float dz = z - piece.Z;
            if (dz < 0f) dz = -dz;
            return dx <= piece.W * 0.5f && dz <= piece.D * 0.5f;
        }

        public static bool BlocksLane(Piece piece, float laneZ)
        {
            if (!piece.Solid || piece.Name == "RoomFloor" || piece.Name == "RoomWall") return false;
            float half = piece.D * 0.5f;
            float low = piece.Z - half;
            float high = piece.Z + half;
            return low < laneZ + 0.9f && high > laneZ - 0.9f;
        }

        private static Piece Furnish(string footprint, float x, float z)
        {
            if (footprint == "clinic" || footprint == "hospital")
                return Box("RoomCot", x + 1.2f, 0.225f, z + 1.6f, 2f, 0.45f, 0.8f, true);
            if (footprint == "warehouse")
                return Box("RoomShelf", x + 1.4f, 0.9f, z - 1.8f, 1.6f, 1.8f, 0.45f, true);
            if (footprint == "station")
                return Box("RoomDesk", x + 1f, 0.375f, z + 1.5f, 1.2f, 0.75f, 0.6f, true);
            if (footprint == "apartment")
                return Box("RoomTable", x + 0.6f, 0.35f, z - 1.6f, 1f, 0.7f, 0.8f, true);
            return Box("RoomCounter", x + 1.4f, 0.5f, z + 1.7f, 1.8f, 1f, 0.45f, true);
        }

        private static Piece Box(string name, float x, float y, float z, float w, float h, float d, bool solid)
        {
            return new Piece { Name = name, X = x, Y = y, Z = z, W = w, H = h, D = d, Solid = solid };
        }
    }
}

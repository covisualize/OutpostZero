namespace OutpostZero.Shell
{
    /// <summary>
    /// Street dressing for a district. Positions stay on the expedition road,
    /// clear of the sanctuary yard and the player's spawn.
    /// </summary>
    public static class DistrictLayout
    {
        public struct Piece
        {
            public string Role;
            public float X;
            public float Z;
            public float Yaw;
        }

        public static Piece[] For(string id)
        {
            switch (id)
            {
                case "rail_yard":
                    return new[]
                    {
                        PieceAt("barrel_explosive", 1.6f, 4f, 0f),
                        PieceAt("barrel_explosive", 2.4f, 7f, 10f),
                        PieceAt("barrel_oil", 3.2f, 10f, 0f),
                        PieceAt("barrel_oil", -2.2f, 8f, 20f),
                        PieceAt("cover", -3.5f, 5f, 90f),
                        PieceAt("cover", 4.5f, 6f, 90f),
                        PieceAt("crate_military", 5.5f, 12f, 15f)
                    };
                case "old_hospital":
                    return new[]
                    {
                        PieceAt("crate_medical", -3.2f, 6f, 20f),
                        PieceAt("crate_medical", 2.8f, 9f, -10f),
                        PieceAt("crate_medical", 4.2f, 13f, 40f),
                        PieceAt("barrel_toxic", 6.2f, 8f, 0f),
                        PieceAt("barrel_toxic", -1.5f, 12f, 0f),
                        PieceAt("cover", 0.5f, 15f, 0f),
                        PieceAt("lamp", -4f, 10f, 0f)
                    };
                case "commercial_strip":
                    return new[]
                    {
                        PieceAt("stall", -4f, 5f, 90f),
                        PieceAt("stall", 4.2f, 8f, 90f),
                        PieceAt("crate", 2.2f, 12f, -8f),
                        PieceAt("lamp", 0f, 16f, 0f)
                    };
                case "police_station":
                    return new[]
                    {
                        PieceAt("cover", -3.2f, 4f, 80f),
                        PieceAt("cover", 3.4f, 5f, 90f),
                        PieceAt("crate_military", 5.5f, 11f, 15f),
                        PieceAt("lamp", -4f, 14f, 0f)
                    };
                case "water_plant":
                    return new[]
                    {
                        PieceAt("barrel_oil", 2.4f, 6f, 0f),
                        PieceAt("barrel_oil", -2.2f, 9f, 20f),
                        PieceAt("cover", 4.5f, 12f, 90f),
                        PieceAt("lamp", 0f, 16f, 0f)
                    };
                case "mall":
                    return new[]
                    {
                        PieceAt("stall", -4f, 6f, 90f),
                        PieceAt("stall", 4.2f, 10f, 90f),
                        PieceAt("crate", -2.5f, 13f, 12f),
                        PieceAt("crate", 2.2f, 8f, -8f)
                    };
                case "highway_overpass":
                    return new[]
                    {
                        PieceAt("cover", -3.2f, 3.5f, 80f),
                        PieceAt("cover", 2.4f, 4.2f, 100f),
                        PieceAt("barrel_explosive", 3.5f, 9f, 0f),
                        PieceAt("crate_military", 6f, 14f, 25f)
                    };
                case "downtown_core":
                    return new[]
                    {
                        PieceAt("cover", -3f, 4f, 80f),
                        PieceAt("cover", 3f, 5f, 90f),
                        PieceAt("crate_military", 5.5f, 12f, 15f),
                        PieceAt("barrel_explosive", -2f, 9f, 0f),
                        PieceAt("lamp", 0f, 16f, 0f)
                    };
                case "north_gate":
                    return new[]
                    {
                        PieceAt("cover", -3.2f, 3.5f, 80f),
                        PieceAt("cover", -0.4f, 4.2f, 70f),
                        PieceAt("cover", 2.4f, 3.6f, 100f),
                        PieceAt("cover", 5f, 4.4f, 80f),
                        PieceAt("barrel_explosive", -2f, 8f, 0f),
                        PieceAt("barrel_explosive", 3.5f, 9f, 0f),
                        PieceAt("crate_military", 6f, 14f, 25f),
                        PieceAt("lamp", 0f, 11f, 0f)
                    };
                default:
                    return new[]
                    {
                        PieceAt("stall", -4f, 6f, 90f),
                        PieceAt("stall", -4f, 10f, 90f),
                        PieceAt("stall", 4.2f, 7f, 90f),
                        PieceAt("stall", 4.2f, 11f, 90f),
                        PieceAt("crate", -2.5f, 8f, 12f),
                        PieceAt("crate", 2.2f, 13f, -8f),
                        PieceAt("lamp", 0f, 16f, 0f)
                    };
            }
        }

        public static int Count(string id, string role)
        {
            int count = 0;
            var pieces = For(id);
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].Role == role) count++;
            }
            return count;
        }

        public static bool StaysOnTheStreet(Piece piece)
        {
            bool clearOfSpawn = piece.X * piece.X + piece.Z * piece.Z > 9f;
            bool clearOfSanctuary = piece.X > -6f || piece.Z > -4f;
            return clearOfSpawn && clearOfSanctuary && piece.Z > 1f && piece.Z < 20f;
        }

        private static Piece PieceAt(string role, float x, float z, float yaw)
        {
            return new Piece { Role = role, X = x, Z = z, Yaw = yaw };
        }
    }
}

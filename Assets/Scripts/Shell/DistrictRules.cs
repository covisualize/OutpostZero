using OutpostZero.Graphics;

namespace OutpostZero.Shell
{
    /// <summary>
    /// What changes when the roster walks into a district: the quota, the opening
    /// horde, the weather, and which crate table the street uses.
    /// </summary>
    public static class DistrictRules
    {
        public struct Profile
        {
            public int KillGoal;
            public int ScrapGoal;
            public float OpeningTension;
            public float SpawnInterval;
            public WeatherKind Weather;
            public string LootTable;
            public string PreferredVariant;
        }

        public static string ActiveTable { get; private set; } = "";

        public static Profile For(string id)
        {
            switch (id)
            {
                case "rail_yard":
                    return new Profile
                    {
                        KillGoal = 12,
                        ScrapGoal = 12,
                        OpeningTension = 28f,
                        SpawnInterval = 7f,
                        Weather = WeatherKind.Rain,
                        LootTable = "military",
                        PreferredVariant = "Brute"
                    };
                case "old_hospital":
                    return new Profile
                    {
                        KillGoal = 10,
                        ScrapGoal = 8,
                        OpeningTension = 18f,
                        SpawnInterval = 8f,
                        Weather = WeatherKind.Fog,
                        LootTable = "medical",
                        PreferredVariant = "Runner"
                    };
                case "north_gate":
                    return new Profile
                    {
                        KillGoal = 16,
                        ScrapGoal = 20,
                        OpeningTension = 42f,
                        SpawnInterval = 5.5f,
                        Weather = WeatherKind.Clear,
                        LootTable = "military",
                        PreferredVariant = "Brute"
                    };
                default:
                    return new Profile
                    {
                        KillGoal = 8,
                        ScrapGoal = 15,
                        OpeningTension = 0f,
                        SpawnInterval = 9f,
                        Weather = WeatherKind.Clear,
                        LootTable = "",
                        PreferredVariant = ""
                    };
            }
        }

        public static void SetActiveTable(string table)
        {
            ActiveTable = table ?? "";
        }
    }
}

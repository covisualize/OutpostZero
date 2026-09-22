namespace OutpostZero.Graphics
{
    /// <summary>
    /// Eye colours, clothing tints, and the gore threshold. Three bodies with
    /// different seeds do not share a tint.
    /// </summary>
    public static class CharacterLook
    {
        public const float GoreLine = 0.4f;
        public const float WalkerStrength = 2.5f;
        public const float RunnerStrength = 2.5f;
        public const float BruteStrength = 1.4f;

        public struct Rgb
        {
            public float R;
            public float G;
            public float B;
        }

        public static string RoleOf(string name)
        {
            if (string.IsNullOrEmpty(name)) return "walker";
            string text = name.ToLowerInvariant();
            if (text.Contains("runner")) return "runner";
            if (text.Contains("brute")) return "brute";
            if (text.Contains("merchant")) return "merchant";
            if (text.Contains("colonist")) return "colonist";
            if (text.Contains("survivor") || text.Contains("leader") || text.Contains("player")) return "survivor";
            return "walker";
        }

        public static bool Glows(string role)
        {
            string id = RoleOf(role);
            return id == "walker" || id == "runner" || id == "brute";
        }

        public static Rgb Eye(string role)
        {
            switch (RoleOf(role))
            {
                case "runner": return new Rgb { R = 0.95f, G = 0.12f, B = 0.08f };
                case "brute": return new Rgb { R = 0.95f, G = 0.45f, B = 0.12f };
                case "survivor": return new Rgb { R = 0.25f, G = 0.18f, B = 0.14f };
                case "merchant": return new Rgb { R = 0.22f, G = 0.16f, B = 0.12f };
                case "colonist": return new Rgb { R = 0.28f, G = 0.2f, B = 0.15f };
                default: return new Rgb { R = 0.75f, G = 1f, B = 0.35f };
            }
        }

        public static float Strength(string role)
        {
            switch (RoleOf(role))
            {
                case "runner": return RunnerStrength;
                case "brute": return BruteStrength;
                case "walker": return WalkerStrength;
                default: return 0.35f;
            }
        }

        public static float EyeHeight(string role)
        {
            switch (RoleOf(role))
            {
                case "runner": return 1.12f;
                case "brute": return 1.96f;
                case "walker": return 1.55f;
                default: return 1.62f;
            }
        }

        public static Rgb Clothing(int seed)
        {
            int outfit = Mod(seed, 3);
            int cap = Mod(seed, 4);
            float r = (Outfit(outfit, 0) / 0.24f) * Channel(seed, 1);
            float g = (Outfit(outfit, 1) / 0.32f) * Channel(seed, 2) * Head(cap);
            float b = (Outfit(outfit, 2) / 0.20f) * Channel(seed, 3);
            return new Rgb { R = r, G = g, B = b };
        }

        public static Rgb Gore(Rgb tint)
        {
            return new Rgb
            {
                R = tint.R * 0.45f + 1.15f * 0.55f,
                G = tint.G * 0.45f + 0.28f * 0.55f,
                B = tint.B * 0.45f + 0.24f * 0.55f
            };
        }

        public static bool Wounded(float current, float maximum)
        {
            return Wounded(current, maximum, 1);
        }

        public static bool Wounded(float current, float maximum, int gore)
        {
            if (maximum <= 0.01f) return false;
            if (gore <= 0) return false;
            float line = gore >= 2 ? 0.7f : GoreLine;
            return current / maximum < line;
        }

        private static float Outfit(int index, int channel)
        {
            if (index == 1) return channel == 0 ? 0.32f : channel == 1 ? 0.24f : 0.18f;
            if (index == 2) return channel == 0 ? 0.18f : channel == 1 ? 0.22f : 0.28f;
            return channel == 0 ? 0.24f : channel == 1 ? 0.32f : 0.20f;
        }

        private static float Head(int index)
        {
            if (index == 1) return 1f;
            if (index == 2) return 1.08f;
            if (index == 3) return 0.82f;
            return 0.90f;
        }

        private static float Channel(int seed, int index)
        {
            uint mixed = unchecked((uint)seed * 747796405u + (uint)index * 2891336453u);
            float unit = (mixed & 0xFFFFu) / 65535f;
            return 0.92f + unit * 0.16f;
        }

        private static int Mod(int seed, int length)
        {
            int index = seed % length;
            if (index < 0) index += length;
            return index;
        }
    }
}

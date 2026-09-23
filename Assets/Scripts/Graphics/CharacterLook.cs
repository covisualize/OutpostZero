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

        public const float OutlinePower = 1.1f;
        public static readonly Rgb RimWarm = new Rgb { R = 0.85f, G = 0.55f, B = 0.28f };
        /// <summary>Faction rims with the colour-vision palette off: survivors cyan, zombies sickly green.</summary>
        public static readonly Rgb RimSurvivor = new Rgb { R = 0.3f, G = 0.85f, B = 1f };
        public static readonly Rgb RimZombie = new Rgb { R = 0.55f, G = 0.85f, B = 0.25f };

        /// <summary>
        /// Enemy eyes and rim in a colour-blind mode: amber for blue-yellow (1), white for mono (2),
        /// pink-red for red-teal (3). Mode 0 keeps the per-archetype eyes and the warm rim.
        /// </summary>
        public static Rgb Enemy(int vision)
        {
            if (vision == 1) return new Rgb { R = 1f, G = 0.78f, B = 0.1f };
            if (vision == 2) return new Rgb { R = 1f, G = 1f, B = 1f };
            if (vision == 3) return new Rgb { R = 1f, G = 0.2f, B = 0.45f };
            return RimWarm;
        }

        public static Rgb Eye(string role, int vision)
        {
            return vision > 0 && Glows(role) ? Enemy(vision) : Eye(role);
        }

        public static void Rim(string role, int vision, bool outline, out Rgb color, out float alpha, out float power)
        {
            if (!Glows(role))
            {
                color = RimSurvivor;
                alpha = 0.35f;
                power = 3.2f;
                return;
            }
            color = vision > 0 ? Enemy(vision) : RimZombie;
            alpha = outline ? 1f : 0.6f;
            power = outline ? OutlinePower : 1.6f;
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

        public const float ZombieGore = 0.22f;
        public const float WoundedGore = 0.6f;
        public const float SurvivorWoundedGore = 0.4f;

        /// <summary>
        /// How much of the body the shader's blood mask covers: zombies always carry some, the
        /// wounded state (the gore variant) soaks them, the gore setting scales or removes it.
        /// </summary>
        public static float GoreAmount(string role, bool wounded, int gore)
        {
            if (gore <= 0) return 0f;
            float amount = Glows(role) ? (wounded ? WoundedGore : ZombieGore) : (wounded ? SurvivorWoundedGore : 0f);
            if (gore >= 2) amount *= 1.25f;
            return amount > 1f ? 1f : amount;
        }

        /// <summary>Where the splats land on this body, in [0, 100); neighbours in a horde differ.</summary>
        public static float GoreSeed(int seed)
        {
            uint mixed = unchecked((uint)seed * 2654435761u ^ 0x9E3779B9u);
            mixed ^= mixed >> 15;
            return (mixed % 10000u) / 100f;
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

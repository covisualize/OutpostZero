namespace OutpostZero.Graphics
{
    /// <summary>
    /// Low through Ultra budgets for zombies, decals, shadows, and sight checks.
    /// Low targets a 30 fps integrated GPU. Medium and above target 60 fps.
    /// </summary>
    public static class QualityProfile
    {
        public struct Tier
        {
            public string Name;
            public int Zombies;
            public int Decals;
            public int Particles;
            public float ShadowDistance;
            public float RenderScale;
            public int Msaa;
            public bool Ssao;
            public bool DepthOfField;
            public bool Grain;
            public float Bloom;
            public float FrameMs;
        }

        public const int SightPerFrame = 8;
        public const float LodSwitch = 28f;
        public const float LodCull = 60f;

        private static int nextToken;

        public static Tier For(int index)
        {
            if (index <= 0)
            {
                return new Tier
                {
                    Name = "Low", Zombies = 16, Decals = 40, Particles = 80,
                    ShadowDistance = 18f, RenderScale = 0.75f, Msaa = 1,
                    Ssao = false, DepthOfField = false, Grain = false, Bloom = 0.12f, FrameMs = 33.3f
                };
            }
            if (index == 2)
            {
                return new Tier
                {
                    Name = "High", Zombies = 32, Decals = 240, Particles = 400,
                    ShadowDistance = 60f, RenderScale = 1f, Msaa = 2,
                    Ssao = true, DepthOfField = true, Grain = true, Bloom = 0.55f, FrameMs = 16.6f
                };
            }
            if (index >= 3)
            {
                return new Tier
                {
                    Name = "Ultra", Zombies = 40, Decals = 400, Particles = 600,
                    ShadowDistance = 80f, RenderScale = 1f, Msaa = 4,
                    Ssao = true, DepthOfField = true, Grain = true, Bloom = 0.7f, FrameMs = 16.6f
                };
            }
            return new Tier
            {
                Name = "Medium", Zombies = 32, Decals = 120, Particles = 220,
                ShadowDistance = 40f, RenderScale = 1f, Msaa = 2,
                Ssao = true, DepthOfField = false, Grain = false, Bloom = 0.35f, FrameMs = 16.6f
            };
        }

        public static int Lod(float distance)
        {
            if (distance >= LodCull) return -1;
            if (distance >= LodSwitch) return 1;
            return 0;
        }

        public static bool Culls(string name, float distance)
        {
            if (string.IsNullOrEmpty(name) || Lod(distance) >= 0) return false;
            if (name == "KitBlock") return true;
            if (!name.StartsWith("Dress_")) return false;
            if (name == "Dress_skyline" || name == "Dress_tower" || name == "Dress_overpass" || name == "Dress_Wire" || name == "Dress_Laundry") return false;
            return true;
        }

        public static int NextToken()
        {
            nextToken++;
            if (nextToken > 100000) nextToken = 1;
            return nextToken;
        }

        public static bool SightDue(int token, int frame)
        {
            int groups = 4;
            int slot = token % groups;
            if (slot < 0) slot += groups;
            int turn = frame % groups;
            if (turn < 0) turn += groups;
            return slot == turn;
        }
    }
}

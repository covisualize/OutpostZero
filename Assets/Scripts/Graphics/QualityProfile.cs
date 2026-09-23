namespace OutpostZero.Graphics
{
    /// <summary>
    /// Low through Ultra budgets for zombies, decals, shadows, and sight checks.
    /// Low targets a 30 fps integrated GPU. Medium and above target 60 fps.
    /// Each tier owns a URP asset (Assets/Settings/OutpostZero_URP_&lt;Name&gt;.asset) and one Unity quality level at the same index.
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
            public int ShadowResolution;
            public int Cascades;
            public float RenderScale;
            public int Msaa;
            public bool Hdr;
            public bool Ssao;
            public bool DepthOfField;
            public bool Grain;
            public float Bloom;
            public float LodBias;
            public float FrameMs;
        }

        public const int Count = 4;
        public const int SightPerFrame = 8;
        public const float LodSwitch = 28f;
        public const float LodCull = 60f;
        public const string SettingsFolder = "Assets/Settings";
        public const string RendererFull = "OutpostZero_URP_Renderer";
        public const string RendererLite = "OutpostZero_URP_Renderer_Lite";

        private static int nextToken;

        public static Tier For(int index)
        {
            if (index <= 0)
            {
                return new Tier
                {
                    Name = "Low", Zombies = OutpostZero.AI.DifficultyProfile.LowTierAlive, Decals = 40, Particles = 80,
                    ShadowDistance = 18f, ShadowResolution = 1024, Cascades = 1, RenderScale = 0.75f, Msaa = 1, Hdr = false,
                    Ssao = false, DepthOfField = false, Grain = false, Bloom = 0.12f, LodBias = 0.8f, FrameMs = 33.3f
                };
            }
            if (index == 2)
            {
                return new Tier
                {
                    Name = "High", Zombies = 32, Decals = 240, Particles = 400,
                    ShadowDistance = 60f, ShadowResolution = 2048, Cascades = 2, RenderScale = 1f, Msaa = 2, Hdr = true,
                    Ssao = true, DepthOfField = true, Grain = true, Bloom = 0.55f, LodBias = 1.25f, FrameMs = 16.6f
                };
            }
            if (index >= 3)
            {
                return new Tier
                {
                    Name = "Ultra", Zombies = 40, Decals = 400, Particles = 600,
                    ShadowDistance = 80f, ShadowResolution = 4096, Cascades = 4, RenderScale = 1f, Msaa = 4, Hdr = true,
                    Ssao = true, DepthOfField = true, Grain = true, Bloom = 0.7f, LodBias = 1.5f, FrameMs = 16.6f
                };
            }
            return new Tier
            {
                Name = "Medium", Zombies = 32, Decals = 120, Particles = 220,
                ShadowDistance = 40f, ShadowResolution = 2048, Cascades = 2, RenderScale = 1f, Msaa = 2, Hdr = true,
                Ssao = true, DepthOfField = false, Grain = false, Bloom = 0.35f, LodBias = 1f, FrameMs = 16.6f
            };
        }

        public static int Clamp(int index) => index < 0 ? 0 : index >= Count ? Count - 1 : index;

        public static string AssetName(int index) => "OutpostZero_URP_" + For(Clamp(index)).Name;

        public static string AssetPath(int index) => SettingsFolder + "/" + AssetName(index) + ".asset";

        /// <summary>Low draws without SSAO, so its asset points at the renderer that lacks the feature.</summary>
        public static string RendererName(int index) => For(Clamp(index)).Ssao ? RendererFull : RendererLite;

        public static string RendererPath(int index) => SettingsFolder + "/" + RendererName(index) + ".asset";

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

        /// <summary>How many frames a full sight sweep takes so that about <see cref="SightPerFrame"/> zombies look each frame.</summary>
        public static int SightGroups(int alive)
        {
            if (alive <= SightPerFrame) return 1;
            return (alive + SightPerFrame - 1) / SightPerFrame;
        }

        public static bool SightDue(int token, int frame) => SightDue(token, frame, 4);

        public static bool SightDue(int token, int frame, int groups)
        {
            if (groups <= 1) return true;
            int slot = token % groups;
            if (slot < 0) slot += groups;
            int turn = frame % groups;
            if (turn < 0) turn += groups;
            return slot == turn;
        }
    }
}

namespace OutpostZero.Core
{
    /// <summary>
    /// How fast each generated gait clip carries a character at 1x playback, so the Animator
    /// can play it at the speed the body really moves and the planted foot stays put.
    /// The numbers come from <c>character_rig.ground_speeds</c>; a Python test keeps them in step.
    /// </summary>
    public static class StrideSheet
    {
        public const string MoveRate = "MoveRate";
        public const float Slowest = 0.4f;
        public const float Fastest = 2.5f;

        /// <summary>Metres per second of the clip at 1x for a model family, or 0 when it has no gait.</summary>
        public static float GroundSpeed(string modelId, string clip)
        {
            bool zombie = !string.IsNullOrEmpty(modelId) && modelId.StartsWith("Zombie_");
            return zombie ? Zombie(modelId, clip) : Survivor(clip);
        }

        private static float Survivor(string clip)
        {
            switch (clip)
            {
                case "Walk": return Speed("Survivor", "Walk", 2.13f);
                case "Sprint": return Speed("Survivor", "Sprint", 5.448f);
                case "CrouchWalk": return Speed("Survivor", "CrouchWalk", 1.389f);
                default: return 0f;
            }
        }

        private static float Zombie(string modelId, string clip)
        {
            switch (clip)
            {
                case "Walk": return Speed("Zombie", "Walk", 1.557f);
                case "Shamble": return Speed("Zombie", "Shamble", 0.848f);
                case "Sprint": return Speed("Zombie", "Sprint", 3.287f);
                case "Charge": return modelId == "Zombie_Brute" ? Speed("Zombie_Brute", "Charge", 5.759f) : 0f;
                default: return 0f;
            }
        }

        private static float Speed(string family, string clip, float metresPerSecond)
        {
            return metresPerSecond;
        }

        /// <summary>
        /// Playback rate that matches the clip's stride to the body's speed. A model scaled up
        /// takes longer strides, so it plays slower for the same speed.
        /// </summary>
        public static float Rate(float speed, float groundSpeed, float scale = 1f)
        {
            if (groundSpeed <= 0f || speed <= 0.05f) return 1f;
            if (scale <= 0.01f) scale = 1f;
            float rate = speed / (groundSpeed * scale);
            if (rate < Slowest) return Slowest;
            return rate > Fastest ? Fastest : rate;
        }

        /// <summary>The Animator's Speed: measured speed over top speed, so 1 is flat out.</summary>
        public static float SpeedParam(float speed, float topSpeed)
        {
            if (topSpeed <= 0.01f || speed <= 0f) return 0f;
            float share = speed / topSpeed;
            return share > 1.5f ? 1.5f : share;
        }

        /// <summary>Switch to the run take once the body is nearer the run's stride than the walk's.</summary>
        public static bool Sprints(float speed, float walkGround, float sprintGround, float scale = 1f)
        {
            if (sprintGround <= 0f) return false;
            if (scale <= 0.01f) scale = 1f;
            return speed > (walkGround + sprintGround) * 0.5f * scale;
        }

        /// <summary>How far the planted foot slides per second at <paramref name="rate"/>; 0 means it holds.</summary>
        public static float Slide(float speed, float groundSpeed, float rate, float scale = 1f)
        {
            float slide = speed - groundSpeed * scale * rate;
            return slide < 0f ? -slide : slide;
        }
    }
}

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A faded crosswalk, a manhole, and a curb grate. Ash Market keeps them on the old marks.
    /// Another district slides the set down the avenue, still off the spawn circle.
    /// </summary>
    public static class LanePaint
    {
        public static float Shift(string districtId)
        {
            if (string.IsNullOrEmpty(districtId) || districtId == "ash_market") return 0f;
            int seed = DressingPlan.SeedFor(districtId);
            if (seed < 0) seed = -seed;
            return ((seed % 4) + 1) * 0.35f;
        }

        public static DressingPlan.Mark[] Marks(string districtId)
        {
            float slide = Shift(districtId);
            float cross = 8f + slide;
            return new[]
            {
                Stripe(-1.2f, cross),
                Stripe(-0.4f, cross),
                Stripe(0.4f, cross),
                Stripe(1.2f, cross),
                Plate("manhole", 2.4f, 5.5f + slide, 0.72f, 0.06f, 0.72f),
                Plate("grate", -2.6f, 11f + slide, 0.34f, 0.05f, 0.9f)
            };
        }

        private static DressingPlan.Mark Stripe(float x, float z)
        {
            return Plate("stripe", x, z, 0.28f, 0.02f, 1.35f);
        }

        private static DressingPlan.Mark Plate(string role, float x, float z, float w, float h, float d)
        {
            return new DressingPlan.Mark
            {
                Role = role,
                X = x,
                Y = 0.02f,
                Z = z,
                Yaw = 0f,
                W = w,
                H = h,
                D = d
            };
        }
    }
}

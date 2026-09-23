namespace OutpostZero.Items
{
    /// <summary>
    /// Pack weight, the heavy line, and how a stack splits. Scrap and medkits use the same
    /// weights the inventory already applies.
    /// </summary>
    public static class PackOps
    {
        public const float ScrapWeight = 0.1f;
        public const float MedkitWeight = 0.5f;
        public const float HeavyLine = 0.9f;
        public const float BaseLimit = 35f;
        public const float RaisedLimit = 50f;
        public const int RaiseScrap = 0;
        public const int RaiseCloth = 6;
        public const int RaiseTape = 3;
        public const float SweepHold = 0.5f;

        public static int Tier(int stored)
        {
            return stored >= 2 ? 2 : 1;
        }

        public static float Limit(int tier)
        {
            return Tier(tier) >= 2 ? RaisedLimit : BaseLimit;
        }

        public static bool CanRaise(int tier, int benchTier, int scrap, int cloth, int tape)
        {
            if (Tier(tier) >= 2) return false;
            if (benchTier < 2) return false;
            if (scrap < RaiseScrap || cloth < RaiseCloth || tape < RaiseTape) return false;
            return true;
        }

        public static float Weight(float itemWeight, int scrap, int medkits)
        {
            if (itemWeight < 0f) itemWeight = 0f;
            if (scrap < 0) scrap = 0;
            if (medkits < 0) medkits = 0;
            return itemWeight + scrap * ScrapWeight + medkits * MedkitWeight;
        }

        public static bool Heavy(float current, float max)
        {
            if (max <= 0.01f) return current > 0f;
            return current / max > HeavyLine;
        }

        public static bool AllowsSprint(float current, float max)
        {
            return !Heavy(current, max);
        }

        public static bool Swept(float heldFor)
        {
            return heldFor >= SweepHold;
        }

        public static bool Fits(float current, float max, float added)
        {
            if (added < 0f) added = 0f;
            if (max <= 0f) return false;
            return current + added <= max + 0.0001f;
        }

        public static int SplitOff(int quantity)
        {
            if (quantity < 2) return 0;
            return quantity / 2;
        }
    }
}

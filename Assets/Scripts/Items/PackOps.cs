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

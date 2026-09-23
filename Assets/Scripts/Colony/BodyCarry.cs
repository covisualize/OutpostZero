namespace OutpostZero.Colony
{
    /// <summary>
    /// Wear on the street comes home on the leader. A camp day can rest it.
    /// Until that first return, the body keeps the fatigue it already has.
    /// </summary>
    public static class BodyCarry
    {
        public static float Clamp(float fatigue)
        {
            if (fatigue < 0f) return 0f;
            if (fatigue > 100f) return 100f;
            return fatigue;
        }

        public static float Carry(float camp, float street, bool known)
        {
            if (!known) return Clamp(street);
            return Clamp(camp);
        }
    }
}

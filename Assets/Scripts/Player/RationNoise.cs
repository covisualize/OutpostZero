namespace OutpostZero.Player
{
    /// <summary>
    /// Opening a ration on the street calls anything close.
    /// The bite sits over a crouch and under a spent casing.
    /// </summary>
    public static class RationNoise
    {
        public const float Radius = 2.6f;
        public const float Loud = 0.22f;

        public static bool Calls(float hunger, float thirst)
        {
            return hunger > 0f || thirst > 0f;
        }
    }
}

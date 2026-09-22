namespace OutpostZero.Sensory
{
    /// <summary>
    /// A bleeding body calls every drip. Gore off stays quiet.
    /// The reach sits under a walk and over a crouch.
    /// </summary>
    public static class BleedScent
    {
        public const float Radius = 4.5f;
        public const float Loud = 0.35f;

        public static bool Calls(bool bleeding, bool alive, int gore)
        {
            return bleeding && alive && Combat.WoundShow.Bleeds(gore);
        }
    }
}

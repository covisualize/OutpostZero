namespace OutpostZero.Combat
{
    /// <summary>
    /// A sprint or a hit drops a reload that has not reached the rack.
    /// Once the action is racking, the magazine stays in.
    /// </summary>
    public static class ReloadBreak
    {
        public const float Rack = 0.72f;

        public static bool Saves(float fill)
        {
            if (fill < 0f) fill = 0f;
            if (fill > 1f) fill = 1f;
            return fill >= Rack;
        }

        public static bool Abort(bool sprinting, bool hit, float fill)
        {
            if (Saves(fill)) return false;
            return sprinting || hit;
        }
    }
}

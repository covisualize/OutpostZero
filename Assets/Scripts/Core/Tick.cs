namespace OutpostZero.Core
{
    /// <summary>
    /// Float slack for inclusive gates. 9.4f - 7f lands just under 2.4f, so a gap
    /// that is due at its edge would miss by one step without it.
    /// </summary>
    public static class Tick
    {
        public const float Slack = 0.0001f;

        public static bool Past(float now, float last, float gap)
        {
            return now - last >= gap - Slack;
        }
    }
}

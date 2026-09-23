namespace OutpostZero.Colony
{
    /// <summary>
    /// A day the camp sent nobody out costs everyone 5 morale at dawn. Day 1 is spared: the camp is new.
    /// </summary>
    public static class IdleDay
    {
        public const float Mood = 5f;

        public static bool Idle(bool wentOut, int endedDay)
        {
            return !wentOut && endedDay > 1;
        }
    }
}

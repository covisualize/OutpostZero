namespace OutpostZero.Expedition
{
    /// <summary>
    /// A follower already at three wounds falls on the next bite.
    /// They come home marked fallen, and they do not swing again.
    /// </summary>
    public static class FollowFall
    {
        public static bool Drops(int held)
        {
            return held >= FollowBite.Cap;
        }

        public static string Line(string name, string language)
        {
            return OutpostZero.Shell.StreetAsk.Fall(name, language);
        }
    }
}

namespace OutpostZero.Graphics
{
    /// <summary>
    /// A fog day that lands on a multiple of three opens overcast.
    /// Rain, clear, storms, day zero, and the market stay on the old cast.
    /// </summary>
    public static class CloudDeck
    {
        public static WeatherKind Lay(WeatherKind kind, int day)
        {
            WeatherKind cast = SkyBand.Cast(kind, day);
            if (cast != WeatherKind.Fog) return cast;
            if (day <= 0) return cast;
            if (day % 3 != 0) return cast;
            return WeatherKind.Overcast;
        }
    }
}

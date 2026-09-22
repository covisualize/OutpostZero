namespace OutpostZero.Colony
{
    /// <summary>
    /// A camp with nobody left standing is over.
    /// </summary>
    public static class CampEnd
    {
        public static bool Wiped(int living)
        {
            return living <= 0;
        }
    }
}

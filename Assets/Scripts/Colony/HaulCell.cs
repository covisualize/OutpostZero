namespace OutpostZero.Colony
{
    /// <summary>
    /// A scavenger at the fourth shift sometimes hauls a lamp cell.
    /// Below that shift the pile stays as it was. Skill 8 hauls on every third salt.
    /// </summary>
    public static class HaulCell
    {
        public static bool Due(int salt, int skill)
        {
            if (skill < 4) return false;
            int span = skill >= 8 ? 3 : 5;
            int n = salt < 0 ? -salt : salt;
            return n % span == 0;
        }
    }
}

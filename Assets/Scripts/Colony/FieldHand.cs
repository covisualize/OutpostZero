namespace OutpostZero.Colony
{
    /// <summary>
    /// The leader's practiced skills change the street.
    /// Below the fourth shift the numbers stay what they were.
    /// </summary>
    public static class FieldHand
    {
        public static float Spread(int skill)
        {
            return Practice.Bonus(skill) > 0 ? 0.85f : 1f;
        }

        public static float Reload(int skill)
        {
            return Practice.Bonus(skill) > 0 ? 0.8f : 1f;
        }

        public static int Scrap(int skill)
        {
            return Practice.Bonus(skill);
        }

        public static int Medkit(int skill)
        {
            return 50 + Practice.Bonus(skill) * 6;
        }

        public static string Dose(int skill)
        {
            return "Medkit used  +" + (Medkit(skill) + HandDepth.Heal(skill)) + " HP";
        }
    }
}

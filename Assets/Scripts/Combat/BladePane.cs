namespace OutpostZero.Combat
{
    /// <summary>
    /// A blade uses its own damage on a pane. A fresh pane falls to a machete and not to a light rake.
    /// </summary>
    public static class BladePane
    {
        public static float Hit(float damage)
        {
            if (damage < 0f) return 0f;
            return damage;
        }

        public static bool Breaks(float damage)
        {
            float hp = OutpostZero.Expedition.PaneGlass.Hp;
            return OutpostZero.Expedition.PaneGlass.Gone(OutpostZero.Expedition.PaneGlass.After(hp, Hit(damage)));
        }
    }
}

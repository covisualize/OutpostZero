using OutpostZero.Shell;

namespace OutpostZero.Player
{
    /// <summary>
    /// Picking a pile off the street is a small noise and a line in the current language.
    /// The grab is quieter than a footstep sprint and louder than nothing.
    /// </summary>
    public static class LootTake
    {
        public const float Noise = 3.5f;
        public const float Volume = 0.32f;

        public static string Sound(LootKind kind)
        {
            if (kind == LootKind.Medkit) return "take_soft";
            if (kind == LootKind.Scrap) return "take_metal";
            return "take_box";
        }

        public static string ItemKey(LootKind kind)
        {
            if (kind == LootKind.Medkit) return "item.medkit";
            if (kind == LootKind.Ammo9mm) return "item.ammo_9mm";
            if (kind == LootKind.AmmoShotgun) return "item.ammo_shells";
            return "item.scrap";
        }

        public static string Line(LootKind kind, int amount, string language)
        {
            int count = amount < 1 ? 1 : amount;
            return Loc.T("camp.picked", language) + " " + Loc.T(ItemKey(kind), language) + " +" + count;
        }
    }
}

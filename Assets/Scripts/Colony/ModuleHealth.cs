using System;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Integrity stays a 0 to 100 percentage; hit points decide how much of it one blow takes.
    /// A 100 hp module loses what the blow deals, a 150 hp one two thirds of it, and a
    /// reinforced (tier 2) wall stands half as much again. Every blow still takes at least one.
    /// </summary>
    public static class ModuleHealth
    {
        public const int Base = 100;
        public const int Reinforced = 2;

        public static int Hp(string kind)
        {
            if (ModuleTable.TryRow(kind, out var row) && row.Hp > 0) return row.Hp;
            return CodeHp(kind);
        }

        public static int CodeHp(string kind)
        {
            switch (kind)
            {
                case "Watchtower":
                case "TradingPost":
                    return 150;
                case "Generator":
                case "Turret":
                    return 120;
                case "Farm":
                case "Crate":
                    return 80;
                case "Lamp":
                    return 60;
                default:
                    return Base;
            }
        }

        public static int Of(string kind, int tier)
        {
            int hp = Hp(kind);
            return tier >= Reinforced && Upgrades(kind) ? hp * 3 / 2 : hp;
        }

        public static int Loss(int amount, int hp)
        {
            if (amount <= 0) return 0;
            if (hp <= 0) hp = Base;
            return Math.Max(1, (int)Math.Round(amount * (double)Base / hp, MidpointRounding.AwayFromZero));
        }

        public static int Hit(int integrity, int amount, int hp)
        {
            if (integrity <= 0) return 0;
            int next = integrity - Loss(amount, hp);
            return next < 0 ? 0 : next;
        }

        /// <summary>Only walls take the reinforced tier; the bench has its own upgrade job.</summary>
        public static bool Upgrades(string kind) => kind == "Barricade";

        /// <summary>A finished tier 1 wall can be reinforced.</summary>
        public static bool CanReinforce(string kind, int site, int integrity, int tier)
        {
            return Upgrades(kind) && BuildSite.Ready(site, integrity) && tier < Reinforced;
        }
    }
}

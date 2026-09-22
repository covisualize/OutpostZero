namespace OutpostZero.Colony
{
    /// <summary>
    /// A spare gun breaks down at the workbench. The last weapon stays in hand.
    /// A blade is not scrap.
    /// </summary>
    public static class StripYield
    {
        public static bool Can(int carried, bool melee, bool bench)
        {
            return bench && carried > 1 && !melee;
        }

        public static int Scrap(string id)
        {
            if (id == "pistol_9mm") return 8;
            if (id == "shotgun_pump") return 12;
            if (id == "rifle_assault") return 16;
            if (id == "smg") return 10;
            return 0;
        }

        public static int Chemicals(string id)
        {
            if (id == "shotgun_pump" || id == "rifle_assault" || id == "smg") return 1;
            return 0;
        }

        public static bool RoomFor(int used, int room, int scrap, int chemicals)
        {
            if (scrap <= 0) return false;
            int scrapFit = CampRoom.Fit(used, CampRoom.Scrap, scrap, room);
            if (scrapFit < scrap) return false;
            if (chemicals <= 0) return true;
            int next = used + scrap * CampRoom.Scrap;
            return CampRoom.Fit(next, CampRoom.Chemicals, chemicals, room) >= chemicals;
        }
    }
}

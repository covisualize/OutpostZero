namespace OutpostZero.Colony
{
    /// <summary>
    /// During a raid the guards leave the yard mark and stand on the line that is being hit.
    /// Barricades on that approach come first. A watchtower is the fallback. They spread along the line.
    /// A breakdown or a bad wound pulls them off. Hunger does not.
    /// </summary>
    public static class GuardStand
    {
        public static string Face(string assigned, float morale, int injury)
        {
            if (morale < 10f) return "Rest";
            if (injury >= 2) return "Medic";
            if (assigned == "Guard") return "Guard";
            if (assigned == "Medic") return "Medic";
            return "Rest";
        }

        public static int Pick(string approach, string[] kinds, float[] x, float[] z, int[] sites, int[] integrity, int slot)
        {
            if (kinds == null || x == null || z == null || sites == null || integrity == null) return -1;
            int boards = Count(approach, kinds, x, z, sites, integrity, true);
            if (boards > 0) return Nth(approach, kinds, x, z, sites, integrity, true, Slot(slot, boards));
            int towers = Count(approach, kinds, x, z, sites, integrity, false);
            if (towers > 0) return Nth(approach, kinds, x, z, sites, integrity, false, Slot(slot, towers));
            return -1;
        }

        public static void Mark(string approach, int slot, string[] kinds, float[] x, float[] z, int[] sites, int[] integrity, out float px, out float pz)
        {
            int pick = Pick(approach, kinds, x, z, sites, integrity, slot);
            bool found = pick >= 0 && x != null && z != null && pick < x.Length && pick < z.Length;
            CampPost.Place("Guard", slot, found ? x[pick] : 0f, found ? z[pick] : 0f, found, out px, out pz);
        }

        public static int Slot(int slot, int posts)
        {
            if (posts <= 0) return 0;
            if (slot < 0) slot = 0;
            return slot % posts;
        }

        private static int Count(string approach, string[] kinds, float[] x, float[] z, int[] sites, int[] integrity, bool boards)
        {
            int count = Length(kinds, x, z, sites, integrity);
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            int found = 0;
            for (int i = 0; i < count; i++)
            {
                if (!Fits(kinds[i], sites[i], integrity[i], boards)) continue;
                if (boards && !RaidPlan.Covers(ax, az, x[i], z[i])) continue;
                found++;
            }
            return found;
        }

        private static int Nth(string approach, string[] kinds, float[] x, float[] z, int[] sites, int[] integrity, bool boards, int nth)
        {
            int count = Length(kinds, x, z, sites, integrity);
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            int seen = 0;
            for (int i = 0; i < count; i++)
            {
                if (!Fits(kinds[i], sites[i], integrity[i], boards)) continue;
                if (boards && !RaidPlan.Covers(ax, az, x[i], z[i])) continue;
                if (seen == nth) return i;
                seen++;
            }
            return -1;
        }

        private static int Length(string[] kinds, float[] x, float[] z, int[] sites, int[] integrity)
        {
            int count = kinds.Length;
            if (x.Length < count) count = x.Length;
            if (z.Length < count) count = z.Length;
            if (sites.Length < count) count = sites.Length;
            if (integrity.Length < count) count = integrity.Length;
            return count;
        }

        private static bool Fits(string kind, int site, int integrity, bool boards)
        {
            if (site != 0 || integrity <= 0) return false;
            return boards ? kind == "Barricade" : kind == "Watchtower";
        }
    }
}

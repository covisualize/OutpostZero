namespace OutpostZero.Colony
{
    /// <summary>
    /// Where a colonist stands for the task they are actually doing.
    /// A finished module of the right kind wins. An open site wins for a builder.
    /// With nothing built, they keep the old yard marks.
    /// </summary>
    public static class CampPost
    {
        public const float Beside = 1.1f;

        public static string KindFor(string action)
        {
            if (action == "Cook") return "Campfire";
            if (action == "Guard") return "Watchtower";
            if (action == "Medic") return "Cot";
            if (action == "Scavenge") return "Crate";
            if (action == CraftQueue.Task) return "Workbench";
            return "";
        }

        public static int Pick(string action, string[] kinds, int[] sites, int[] integrity, int[] jobs)
        {
            if (kinds == null || sites == null || integrity == null) return -1;
            int count = kinds.Length;
            if (sites.Length < count) count = sites.Length;
            if (integrity.Length < count) count = integrity.Length;
            if (action == "Build")
            {
                for (int i = 0; i < count; i++)
                {
                    if (sites[i] != 0 && integrity[i] > 0) return i;
                }
                if (jobs == null) return -1;
                int jobsCount = count < jobs.Length ? count : jobs.Length;
                for (int i = 0; i < jobsCount; i++)
                {
                    if (kinds[i] == "Workbench" && sites[i] == 0 && integrity[i] > 0 && jobs[i] > 0 && jobs[i] < CraftGate.Done)
                        return i;
                }
                return -1;
            }
            if (action == "Clear") return -1;
            string kind = KindFor(action);
            if (kind.Length == 0) return -1;
            for (int i = 0; i < count; i++)
            {
                if (kinds[i] == kind && sites[i] == 0 && integrity[i] > 0) return i;
            }
            return -1;
        }

        public static void Fallback(string action, out float x, out float z)
        {
            if (action == "Cook")
            {
                x = -12f;
                z = -12f;
                return;
            }
            if (action == "Guard")
            {
                x = -8f;
                z = -12f;
                return;
            }
            if (action == "Medic")
            {
                x = -16f;
                z = -10f;
                return;
            }
            if (action == "Scavenge")
            {
                x = -18f;
                z = -8f;
                return;
            }
            if (action == "Clear")
            {
                x = -6f;
                z = -8f;
                return;
            }
            x = -14f;
            z = -15f;
        }

        public static void Place(string action, int index, float moduleX, float moduleZ, bool found, out float x, out float z)
        {
            float baseX;
            float baseZ;
            if (found)
            {
                baseX = moduleX + Beside;
                baseZ = moduleZ;
            }
            else Fallback(action, out baseX, out baseZ);
            CampRoutine.Nudge(index, out float nudgeX, out float nudgeZ);
            x = baseX + nudgeX;
            z = baseZ + nudgeZ;
        }
    }
}

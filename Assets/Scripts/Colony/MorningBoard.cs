namespace OutpostZero.Colony
{
    /// <summary>
    /// The morning after a raid puts a free colonist on the bodies, then on the broken boards.
    /// Cooks and medics stay where they are. The leader is not pulled off the gate.
    /// </summary>
    public static class MorningBoard
    {
        public static int Apply(string[] tasks, bool[] alive, bool[] leader, bool bodies, bool damaged)
        {
            if (tasks == null || alive == null || leader == null) return 0;
            int count = tasks.Length;
            if (alive.Length < count) count = alive.Length;
            if (leader.Length < count) count = leader.Length;
            int flags = 0;
            bool haveClear = Has(tasks, alive, leader, count, "Clear");
            bool haveBuild = Has(tasks, alive, leader, count, "Build");
            if (bodies && !haveClear)
            {
                int free = FirstFree(tasks, alive, leader, count);
                if (free >= 0)
                {
                    tasks[free] = "Clear";
                    flags |= 1;
                }
            }
            if (damaged && !haveBuild)
            {
                int free = FirstFree(tasks, alive, leader, count);
                if (free >= 0)
                {
                    tasks[free] = "Build";
                    flags |= 2;
                }
            }
            return flags;
        }

        public static string Key(int flags)
        {
            if (flags == 3) return "camp.morning";
            if ((flags & 1) != 0) return "camp.haul";
            if ((flags & 2) != 0) return "camp.mend";
            return "";
        }

        public static bool Free(string task)
        {
            return task == "Rest" || task == "Guard" || task == "Scavenge" || string.IsNullOrEmpty(task);
        }

        private static bool Has(string[] tasks, bool[] alive, bool[] leader, int count, string task)
        {
            for (int i = 0; i < count; i++)
            {
                if (!alive[i] || leader[i]) continue;
                if (tasks[i] == task) return true;
            }
            return false;
        }

        private static int FirstFree(string[] tasks, bool[] alive, bool[] leader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!alive[i] || leader[i]) continue;
                if (Free(tasks[i])) return i;
            }
            return -1;
        }
    }
}

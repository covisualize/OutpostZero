using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// How well the wall covers the yard. Each of the four raid approaches scores 0 to 100 from the integrity of the
    /// finished barricades within its reach, two whole barricades filling a side. The yard's score is the four averaged.
    /// A gappy wall draws a bigger crowd, and a side's cover takes pressure off the zombies hitting it.
    /// </summary>
    public static class Perimeter
    {
        public const int FullSide = 200;
        public static readonly string[] Sides = { "gate", "alley", "yard", "fence" };

        public struct Wall
        {
            public float X;
            public float Z;
            public int Integrity;
            public bool Ready;
        }

        public static int Side(string approach, IList<Wall> walls)
        {
            if (walls == null) return 0;
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            int held = 0;
            for (int i = 0; i < walls.Count; i++)
            {
                var wall = walls[i];
                if (!wall.Ready || wall.Integrity <= 0) continue;
                if (!RaidPlan.Covers(ax, az, wall.X, wall.Z)) continue;
                held += wall.Integrity > 100 ? 100 : wall.Integrity;
            }
            if (held >= FullSide) return 100;
            return held * 100 / FullSide;
        }

        public static int Score(IList<Wall> walls)
        {
            int sum = 0;
            for (int i = 0; i < Sides.Length; i++) sum += Side(Sides[i], walls);
            return (sum + Sides.Length / 2) / Sides.Length;
        }

        /// <summary>The side with the least cover, first in raid order on a tie.</summary>
        public static string Weakest(IList<Wall> walls)
        {
            string weakest = Sides[0];
            int low = int.MaxValue;
            for (int i = 0; i < Sides.Length; i++)
            {
                int side = Side(Sides[i], walls);
                if (side >= low) continue;
                low = side;
                weakest = Sides[i];
            }
            return weakest;
        }

        /// <summary>Two more bodies through an open yard, none at half cover, two fewer against a full wall. Never under 3.</summary>
        public static int Crowd(int count, int score)
        {
            if (score < 0) score = 0;
            if (score > 100) score = 100;
            int shift = (50 - score) / 25;
            int crowd = count + shift;
            return crowd < 3 ? 3 : crowd;
        }

        /// <summary>A covered side takes up to two off each strike's pressure. Never under 1.</summary>
        public static int Pressure(int pressure, int side)
        {
            if (side < 0) side = 0;
            if (side > 100) side = 100;
            int eased = pressure - side / 34;
            return eased < 1 ? 1 : eased;
        }
    }
}

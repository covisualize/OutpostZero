using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Whether the snapped cell under the pointer takes the selected module: free, inside the fence, and
    /// affordable in scrap and supplies. The ghost wears the verdict's colour before the click, and placement refuses the same cases.
    /// </summary>
    public static class BuildGhost
    {
        public enum Verdict
        {
            Ok,
            Taken,
            Outside,
            Short
        }

        public static readonly Color Good = new Color(0.35f, 0.95f, 0.45f, 0.35f);
        public static readonly Color Bad = new Color(0.95f, 0.3f, 0.25f, 0.35f);

        public static float Snap(float value, float cell)
        {
            if (cell <= 0f) return value;
            return Mathf.Round(value / cell) * cell;
        }

        public static Verdict Check(IReadOnlyList<PlacedModule> placed, float x, float z, int cost, int scrap)
        {
            if (!MapRim.Inside(x, z)) return Verdict.Outside;
            if (placed != null)
            {
                for (int i = 0; i < placed.Count; i++)
                    if (Mathf.Abs(placed[i].x - x) < 0.01f && Mathf.Abs(placed[i].z - z) < 0.01f) return Verdict.Taken;
            }
            if (scrap < cost) return Verdict.Short;
            return Verdict.Ok;
        }

        /// <summary>The same check against a full bill and the camp's stores; no stores means nothing is affordable.</summary>
        public static Verdict Check(IReadOnlyList<PlacedModule> placed, float x, float z, ModuleBill bill, ColonyStorage storage)
        {
            var verdict = Check(placed, x, z, 0, 0);
            if (verdict != Verdict.Ok) return verdict;
            if (storage == null || !bill.Affords(storage.Scrap, storage.Cloth, storage.Chemicals, storage.Tape)) return Verdict.Short;
            return Verdict.Ok;
        }

        public static Color Tint(Verdict verdict) => verdict == Verdict.Ok ? Good : Bad;
    }
}

using System;
using System.Collections.Generic;
using OutpostZero.Expedition;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The faction quests, each an expedition objective: the Clinic's is a delivery of meds at the stall, the
    /// Free Farmers' clears a district nest, and the Caravan's walks its porter to the gate on a visit day. The
    /// field quests join the expedition board as bonus objectives and pay out when the leader extracts with
    /// them done. A <see cref="FactionDefinition"/> carries its quest as an <see cref="ObjectiveDefinition"/>.
    /// </summary>
    public static class FactionQuest
    {
        public const string Prefix = "quest.";
        public const string Porter = "caravan";

        public static ObjectiveSpec Code(string faction)
        {
            switch (faction)
            {
                case "clinic": return new ObjectiveSpec("quest.clinic", ObjectiveKind.Collect, "Medical", 10, true, 0, "quest.clinic", "The Clinic wants 10 meds");
                case "farmers": return new ObjectiveSpec("quest.farmers", ObjectiveKind.ClearNest, "nest", 5, true, 0, "quest.farmers", "Clear a nest for the Free Farmers");
                case "caravan": return new ObjectiveSpec("quest.caravan", ObjectiveKind.Escort, Porter, 1, true, 0, "quest.caravan", "Walk the caravan porter to the gate");
                default: return default;
            }
        }

        public static int CodeStanding(string faction)
        {
            return faction == "clinic" ? 15 : faction == "farmers" || faction == "caravan" ? 10 : 0;
        }

        public static string CodePrint(string faction) => faction == "clinic" ? "dressing" : "";

        public static bool Has(ObjectiveSpec spec) => !string.IsNullOrEmpty(spec.Id);

        /// <summary>The faction's quest from the loaded book, or the built-in one when the book lacks the faction.</summary>
        public static ObjectiveSpec For(string faction)
        {
            if (FactionTable.TryRow(faction, out var row)) return row.Quest;
            return Code(faction);
        }

        /// <summary>The faction a quest objective id belongs to, or "" for an ordinary objective.</summary>
        public static string FactionOf(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId) || !objectiveId.StartsWith(Prefix, StringComparison.Ordinal)) return "";
            string faction = objectiveId.Substring(Prefix.Length);
            return FactionTable.ValidId(faction) && CaravanBook.IndexOf(faction) >= 0 ? faction : "";
        }

        /// <summary>A quest that is played out in a district rather than handed in at the stall.</summary>
        public static bool Field(ObjectiveSpec spec) => Has(spec) && spec.Kind != ObjectiveKind.Collect;

        /// <summary>
        /// The open field quests a run carries: every faction's unfinished field quest, the porter's only on a
        /// day the caravan is in the district. Each keeps the faction's id behind <see cref="Prefix"/>.
        /// </summary>
        public static List<ObjectiveSpec> Open(string done, bool caravanHere)
        {
            var list = new List<ObjectiveSpec>();
            foreach (string faction in CaravanBook.Ids)
            {
                var spec = For(faction);
                if (!Field(spec) || CaravanBook.QuestDone(done, faction)) continue;
                if (spec.Kind == ObjectiveKind.Escort && !caravanHere) continue;
                list.Add(Stamp(spec, faction));
            }
            return list;
        }

        /// <summary>The factions whose quests are done on the board and not yet paid, in board order.</summary>
        public static List<string> Earned(ObjectiveBoard board, string done)
        {
            var list = new List<string>();
            if (board == null) return list;
            for (int i = 0; i < board.Count; i++)
            {
                string faction = FactionOf(board.Spec(i).Id);
                if (faction.Length == 0 || !board.Done(i) || CaravanBook.QuestDone(done, faction) || list.Contains(faction)) continue;
                list.Add(faction);
            }
            return list;
        }

        /// <summary>Carried meds: medkits and every Medical item in the pack.</summary>
        public static int Meds(int kits, IList<KeyValuePair<string, int>> medical)
        {
            int total = Math.Max(0, kits);
            if (medical != null)
                for (int i = 0; i < medical.Count; i++) total += Math.Max(0, medical[i].Value);
            return total;
        }

        /// <summary>
        /// What a hand-in of <paramref name="need"/> meds takes: loose Medical items first in pack order, then
        /// medkits. Returns false and takes nothing when the pack is short.
        /// </summary>
        public static bool Split(int need, int kits, IList<KeyValuePair<string, int>> medical, List<KeyValuePair<string, int>> items, out int kitsTaken)
        {
            kitsTaken = 0;
            items?.Clear();
            if (need <= 0 || Meds(kits, medical) < need) return false;
            int left = need;
            if (medical != null)
                for (int i = 0; i < medical.Count && left > 0; i++)
                {
                    int take = Math.Min(left, Math.Max(0, medical[i].Value));
                    if (take <= 0) continue;
                    items?.Add(new KeyValuePair<string, int>(medical[i].Key, take));
                    left -= take;
                }
            kitsTaken = left;
            return true;
        }

        private static ObjectiveSpec Stamp(ObjectiveSpec spec, string faction)
        {
            string id = Prefix + faction;
            if (spec.Id == id) return spec;
            return new ObjectiveSpec(id, spec.Kind, spec.Target, spec.Count, true, spec.Reward, spec.Key, spec.Fallback);
        }
    }
}

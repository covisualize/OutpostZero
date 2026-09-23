using System;
using System.Collections.Generic;
using System.Globalization;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The results screen's lines past the haul: what came home that didn't go out, the leader's condition,
    /// and the practice the street gave.
    /// </summary>
    public static class ResultsSheet
    {
        /// <summary>
        /// A leader who extracts gets one Guard shift for meeting the kill quota and one Scavenge shift for
        /// meeting the scrap quota, trained exactly like a camp shift.
        /// </summary>
        public static void Earned(ExpeditionOutcome outcome, out int combat, out int scavenge)
        {
            combat = 0;
            scavenge = 0;
            if (outcome.end != ExpeditionEnd.Extracted && outcome.end != ExpeditionEnd.Victory) return;
            if (outcome.kills >= outcome.killGoal) combat = 1;
            if (outcome.scrap >= outcome.scrapGoal) scavenge = 1;
        }

        /// <summary>
        /// Records the objectives met. Their rewards count only when the leader extracts; a run that ends any other
        /// way brings nothing home.
        /// </summary>
        public static ExpeditionOutcome Scored(ExpeditionOutcome outcome, ObjectiveBoard board)
        {
            outcome.objectivesDone = board != null ? board.DoneCount : 0;
            outcome.objectivesTotal = board != null ? board.Count : 0;
            bool home = outcome.end == ExpeditionEnd.Extracted || outcome.end == ExpeditionEnd.Victory;
            outcome.bonusScrap = home && board != null ? board.BonusReward : 0;
            return outcome;
        }

        public static string ObjectivesLine(ExpeditionOutcome outcome, string language)
        {
            if (outcome.objectivesTotal <= 0) return "";
            string line = Word("result.objectives", language) + " " + outcome.objectivesDone + "/" + outcome.objectivesTotal;
            return outcome.bonusScrap > 0 ? line + "  +" + outcome.bonusScrap + " " + Word("result.bonus", language) : line;
        }

        public static ExpeditionOutcome Trained(ExpeditionOutcome outcome, Survivor leader)
        {
            Earned(outcome, out int combat, out int scavenge);
            if (leader == null || !leader.alive) return outcome;
            for (int i = 0; i < combat; i++) Practice.Train(ref leader.combat, ref leader.combatXp);
            for (int i = 0; i < scavenge; i++) Practice.Train(ref leader.scavenge, ref leader.scavengeXp);
            outcome.combatShifts = combat;
            outcome.scavengeShifts = scavenge;
            outcome.combatLevel = leader.combat;
            outcome.scavengeLevel = leader.scavenge;
            return outcome;
        }

        /// <summary>Items in the pack now whose id was not in the loadout, summed per id in id order.</summary>
        public static List<KeyValuePair<string, int>> Brought(string[] loadout, IEnumerable<KeyValuePair<string, int>> pack)
        {
            var went = new HashSet<string>(loadout ?? Array.Empty<string>(), StringComparer.Ordinal);
            var totals = new SortedDictionary<string, int>(StringComparer.Ordinal);
            if (pack != null)
            {
                foreach (var stack in pack)
                {
                    if (string.IsNullOrEmpty(stack.Key) || stack.Value <= 0 || went.Contains(stack.Key)) continue;
                    totals.TryGetValue(stack.Key, out int held);
                    totals[stack.Key] = held + stack.Value;
                }
            }
            return new List<KeyValuePair<string, int>>(totals);
        }

        public static string BroughtLine(List<KeyValuePair<string, int>> brought, string language)
        {
            string head = Word("result.brought", language);
            if (brought == null || brought.Count == 0) return head + " " + Word("result.brought_none", language);
            var text = new System.Text.StringBuilder(head);
            for (int i = 0; i < brought.Count; i++)
            {
                text.Append(i == 0 ? " " : ", ");
                text.Append(string.IsNullOrEmpty(language) ? Loc.Item(brought[i].Key) : Loc.Item(brought[i].Key, language));
                if (brought[i].Value > 1) text.Append(" x").Append(brought[i].Value.ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        public static string ConditionLine(string name, float health, float maxHealth, bool bleeding, int injury, string language)
        {
            int percent = maxHealth > 0f ? (int)Math.Round(100f * Math.Max(0f, Math.Min(health, maxHealth)) / maxHealth) : 0;
            var text = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(name)) text.Append(name).Append(": ");
            text.Append(Word("result.health", language)).Append(" ").Append(percent.ToString(CultureInfo.InvariantCulture)).Append("%");
            if (bleeding) text.Append(", ").Append(Word("hud.bleeding", language).ToLowerInvariant());
            string wound = WoundCard.Line(injury, language);
            if (wound.Length > 0) text.Append(", ").Append(wound);
            if (!bleeding && wound.Length == 0 && percent >= 100) text.Append(", ").Append(Word("result.unhurt", language));
            return text.ToString();
        }

        public static string PracticeLine(ExpeditionOutcome outcome, string language)
        {
            string head = Word("result.practice", language);
            if (outcome.combatShifts <= 0 && outcome.scavengeShifts <= 0) return head + " " + Word("result.practice_none", language);
            var text = new System.Text.StringBuilder(head);
            bool first = true;
            if (outcome.combatShifts > 0) Skill(text, ref first, Loc.Task("Guard", language), outcome.combatShifts, outcome.combatLevel);
            if (outcome.scavengeShifts > 0) Skill(text, ref first, Loc.Task("Scavenge", language), outcome.scavengeShifts, outcome.scavengeLevel);
            return text.ToString();
        }

        private static void Skill(System.Text.StringBuilder text, ref bool first, string label, int shifts, int level)
        {
            text.Append(first ? " " : ", ").Append(label).Append(" +").Append(shifts.ToString(CultureInfo.InvariantCulture))
                .Append(" (").Append(level.ToString(CultureInfo.InvariantCulture)).Append(")");
            first = false;
        }

        private static string Word(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Loc.T(key) : Loc.T(key, language);
        }
    }
}

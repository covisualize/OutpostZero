using System.Collections.Generic;
using System.Globalization;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// One plain line per survivor for the F4 dev roster: who leads, traits, skills, needs, wounds and task.
    /// </summary>
    public static class RosterSheet
    {
        public static string[] Lines(IReadOnlyList<Survivor> survivors, string language)
        {
            if (survivors == null) return new string[0];
            var lines = new string[survivors.Count];
            for (int i = 0; i < survivors.Count; i++) lines[i] = Line(survivors[i], language);
            return lines;
        }

        public static string Line(Survivor survivor, string language)
        {
            if (survivor == null) return "";
            var text = new System.Text.StringBuilder();
            if (survivor.leader) text.Append("[").Append(Loc.T("dev.lead", language)).Append("] ");
            text.Append(survivor.displayName);
            if (survivor.age > 0) text.Append(" (").Append(Number(survivor.age)).Append(")");
            string traits = Traits(survivor);
            if (traits.Length > 0) text.Append(" - ").Append(traits);
            if (!survivor.alive) return text.Append(" | ").Append(Loc.T("dev.dead", language)).ToString();

            string skills = "";
            Skill(ref skills, Loc.Task("Guard", language), survivor.combat, survivor.combatXp);
            Skill(ref skills, Loc.Task("Medic", language), survivor.medicine, survivor.medicineXp);
            Skill(ref skills, Loc.Task("Build", language), survivor.engineering, survivor.engineeringXp);
            Skill(ref skills, Loc.Task("Cook", language), survivor.cooking, survivor.cookingXp);
            Skill(ref skills, Loc.Task("Scavenge", language), survivor.scavenge, survivor.scavengeXp);
            Skill(ref skills, Loc.T("camp.leads", language), survivor.leadership, survivor.leadershipXp);
            if (skills.Length > 0) text.Append(" | ").Append(skills);

            text.Append(" | ").Append(Loc.T("hud.hunger", language)).Append(" ").Append(Number(survivor.hunger))
                .Append("  ").Append(Loc.T("hud.thirst", language)).Append(" ").Append(Number(survivor.thirst))
                .Append("  ").Append(Loc.T("hud.fatigue", language)).Append(" ").Append(Number(survivor.fatigue))
                .Append("  ").Append(Loc.T("camp.morale", language)).Append(" ").Append(Number(survivor.morale));
            if (survivor.injury > 0) text.Append(" | ").Append(Loc.T("dev.hurt", language)).Append(" ").Append(Number(survivor.injury));
            text.Append(" | ").Append(Loc.Task(survivor.task, language))
                .Append(" | ").Append(Loc.T("camp.opinion", language)).Append(" ").Append(Number(survivor.opinion));
            return text.ToString();
        }

        /// <summary>A skill as "Guard 5 1/3": the level, then experience toward the next one. A skill at the cap shows no fraction.</summary>
        private static void Skill(ref string text, string label, int skill, int xp)
        {
            if (skill <= 0 && xp <= 0) return;
            if (text.Length > 0) text += "  ";
            text += label + " " + Number(skill);
            if (skill < Practice.Cap) text += " " + Number(xp) + "/" + Number(Practice.Need(skill));
        }

        private static string Traits(Survivor survivor)
        {
            string text = "";
            foreach (var trait in new[] { survivor.trait, survivor.aside, survivor.mark })
            {
                if (string.IsNullOrEmpty(trait)) continue;
                if (text.Length > 0) text += ", ";
                text += Loc.Trait(trait);
            }
            return text;
        }

        private static string Number(float value)
        {
            return ((int)System.Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        }
    }
}

using System;
using System.Text;

namespace OutpostZero.Colony
{
    /// <summary>
    /// What a camp recipe spends, which station it needs, and who has to be on duty.
    /// Scrap from a built workbench is one cheaper, down to a floor of one.
    /// </summary>
    public static class CraftBill
    {
        public const int Any = 0;
        public const int Workbench = 1;
        public const int Cot = 2;

        public struct Cost
        {
            public int Scrap;
            public int Cloth;
            public int Chemicals;
            public int Tape;
            public int Station;
            public string Skill;
        }

        public static bool TryOf(string id, out Cost cost)
        {
            cost = new Cost();
            if (id == "bandage") cost = Make(1, 2, 0, 0, Any, "");
            else if (id == "medkit") cost = Make(8, 1, 1, 1, Cot, "Medic");
            else if (id == "antibiotics") cost = Make(10, 0, 2, 0, Cot, "Medic");
            else if (id == "painkillers") cost = Make(5, 0, 1, 0, Any, "");
            else if (id == "ammo_9mm") cost = Make(4, 0, 1, 0, Workbench, "");
            else if (id == "ammo_shells") cost = Make(5, 0, 1, 0, Workbench, "");
            else if (id == "ammo_rifle") cost = Make(7, 0, 1, 0, Workbench, "");
            else if (id == "ammo_smg") cost = Make(6, 0, 1, 0, Workbench, "");
            else if (id == "noise_lure") cost = Make(2, 0, 0, 0, Any, "");
            else if (id == "molotov") cost = Make(6, 1, 0, 0, Any, "");
            else if (id == "pipe_bomb") cost = Make(8, 0, 1, 1, Workbench, "");
            else if (id == "suppressor") cost = Make(12, 0, 0, 0, Workbench, "");
            else if (id == "optic") cost = Make(9, 0, 0, 0, Workbench, "");
            else if (id == "extended_mag") cost = Make(8, 0, 0, 0, Workbench, "");
            else if (id == "dressing") cost = Make(2, 2, 0, 0, Any, "");
            else if (id == "flare") cost = Make(4, 0, 1, 0, Workbench, "");
            else if (id == "repair_kit") cost = Make(6, 0, 1, 1, Workbench, "");
            else if (id == "barricade_kit") cost = Make(8, 0, 0, 2, Workbench, "");
            else if (id == "radio_spare") cost = Make(12, 0, 2, 1, Workbench, "");
            else if (id == "cell") cost = Make(3, 0, 1, 0, Workbench, "");
            else return false;
            return true;
        }

        public static int ScrapDue(int scrap, bool workbench)
        {
            if (!workbench) return scrap;
            return scrap <= 1 ? 1 : scrap - 1;
        }

        public static bool StationReady(int station, bool workbench, bool cot)
        {
            if (station == Workbench) return workbench;
            if (station == Cot) return cot;
            return true;
        }

        public static bool OnDuty(string trait, string task, bool alive, string skill)
        {
            if (!alive) return false;
            if (skill == "Medic")
            {
                if (task == "Medic") return true;
                return trait != null && trait.IndexOf("Medic", StringComparison.Ordinal) >= 0;
            }
            return false;
        }

        public static string Block(int station, string skill, bool stationOk, bool skillOk)
        {
            if (!stationOk)
            {
                if (station == Workbench) return "Need a workbench";
                if (station == Cot) return "Need a medical cot";
                return "Need a station";
            }
            if (!skillOk) return "Need a medic on duty";
            return "";
        }

        public static bool Afford(int scrapDue, int cloth, int chemicals, int tape, int haveScrap, int haveCloth, int haveChemicals, int haveTape)
        {
            return haveScrap >= scrapDue && haveCloth >= cloth && haveChemicals >= chemicals && haveTape >= tape;
        }

        public static string Line(string label, int scrap, int cloth, int chemicals, int tape)
        {
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrEmpty(label) ? "Craft" : label);
            if (scrap > 0) builder.Append("   scrap ").Append(scrap);
            if (cloth > 0) builder.Append("   cloth ").Append(cloth);
            if (chemicals > 0) builder.Append("   chem ").Append(chemicals);
            if (tape > 0) builder.Append("   tape ").Append(tape);
            return builder.ToString();
        }

        public static void Salvage(int salt, bool scrounger, out int cloth, out int chemicals, out int tape)
        {
            uint roll = 2166136261u;
            unchecked
            {
                roll ^= (uint)salt;
                roll *= 16777619u;
            }
            cloth = 1 + (scrounger ? 1 : 0);
            chemicals = roll % 4u == 0u ? 1 : 0;
            tape = roll % 5u == 0u ? 1 : 0;
        }

        private static Cost Make(int scrap, int cloth, int chemicals, int tape, int station, string skill)
        {
            return new Cost
            {
                Scrap = scrap,
                Cloth = cloth,
                Chemicals = chemicals,
                Tape = tape,
                Station = station,
                Skill = skill
            };
        }
    }
}

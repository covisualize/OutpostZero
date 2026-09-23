using System;
using OutpostZero.Shell;

namespace OutpostZero.Items
{
    /// <summary>
    /// One readable line for a pack row: name, weight, what it does, and a short note.
    /// </summary>
    public static class ItemBrief
    {
        public static string Text(ItemRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.Id)) return "";
            string line = Loc.Item(record.Id);
            line += "   " + Weight(record.Weight);
            string effect = Effect(record);
            if (!string.IsNullOrEmpty(effect)) line += "   " + effect;
            string blurb = Blurb(record.Id);
            if (!string.IsNullOrEmpty(blurb)) line += "\n" + blurb;
            return line;
        }

        public static string Weight(float kg)
        {
            if (kg < 0f) kg = 0f;
            int grams = (int)Math.Round(kg * 100.0, MidpointRounding.AwayFromZero);
            int whole = grams / 100;
            int frac = grams % 100;
            return whole + "." + (frac < 10 ? "0" : "") + frac + " kg";
        }

        public static string Offer(string verb, string name, int count, float each)
        {
            if (count < 1) count = 1;
            string line = verb + " " + (count > 1 ? count + "× " : "") + name;
            float kg = each * count;
            return kg > 0f ? line + " (" + Weight(kg) + ")" : line;
        }

        public static string Effect(ItemRecord record)
        {
            if (record == null) return "";
            if (record.Heal > 0) return "+" + record.Heal + " " + Loc.T("unit.health");
            if (record.Hunger > 0f) return "+" + (int)record.Hunger + " " + Loc.T("unit.hunger");
            if (record.Thirst > 0f) return "+" + (int)record.Thirst + " " + Loc.T("unit.thirst");
            if (record.Use == ItemUse.Ammo) return record.AmmoAmount + " " + Loc.T("unit.rounds");
            if (record.Use == ItemUse.Cure) return Loc.T("unit.cure");
            if (record.Use == ItemUse.Relief) return Loc.T("unit.relief");
            if (record.Use == ItemUse.Lure) return Loc.T("unit.lure");
            if (record.Use == ItemUse.Molotov) return Loc.T("unit.molotov");
            if (record.Use == ItemUse.Flare) return Loc.T("unit.flare");
            if (record.Use == ItemUse.Bomb) return Loc.T("unit.pipe_bomb");
            if (record.Use == ItemUse.Cell) return Loc.T("unit.cell");
            if (record.Id == "cloth") return Loc.T("unit.cloth");
            if (record.Id == "chemicals") return Loc.T("unit.chemicals");
            if (record.Id == "tape") return Loc.T("unit.tape");
            if (record.Use == ItemUse.Material) return Loc.T("unit.material");
            return "";
        }

        public static string Blurb(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string key = "blurb." + id;
            string line = Loc.T(key);
            return line == key ? "" : line;
        }
    }
}

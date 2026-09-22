using System;

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
            string line = string.IsNullOrEmpty(record.DisplayName) ? record.Id : record.DisplayName;
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

        public static string Effect(ItemRecord record)
        {
            if (record == null) return "";
            if (record.Heal > 0) return "+" + record.Heal + " health";
            if (record.Hunger > 0f) return "+" + (int)record.Hunger + " hunger";
            if (record.Thirst > 0f) return "+" + (int)record.Thirst + " thirst";
            if (record.Use == ItemUse.Ammo) return record.AmmoAmount + " rounds";
            if (record.Use == ItemUse.Cure) return "Clears infection";
            if (record.Use == ItemUse.Relief) return "+20 health over 20s";
            if (record.Use == ItemUse.Lure) return "Draws the dead";
            if (record.Use == ItemUse.Molotov) return "Fire on impact";
            if (record.Use == ItemUse.Material) return "Camp scrap";
            return "";
        }

        public static string Blurb(string id)
        {
            switch (id)
            {
                case "medkit": return "Stops bleeding and breaks a fever.";
                case "bandage": return "Stops bleeding.";
                case "antibiotics": return "Works before the fever turns lethal.";
                case "painkillers": return "A slow mend, not a cure.";
                case "canned_food": return "Heavy, and it keeps.";
                case "water": return "One bottle covers about ten minutes.";
                case "ammo_9mm": return "Fits the pistol.";
                case "ammo_shells": return "Fits the shotgun.";
                case "ammo_rifle": return "Fits the rifle.";
                case "scrap": return "The camp spends this.";
                case "noise_lure": return "Throw it to pull a horde off a door.";
                case "molotov": return "Breaks into fire.";
                default: return "";
            }
        }
    }
}

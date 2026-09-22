using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using OutpostZero.Colony;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Upgrades an older save one schema step at a time until it matches <see cref="SaveCodec.CurrentSchema"/>.
    /// Each step only reads fields that existed in the schema it upgrades from.
    /// </summary>
    public static class SaveMigrations
    {
        public const float UntrackedNeed = 78f;
        public const int BondOpinion = 18;

        private static readonly Func<SaveGameData, SaveGameData>[] steps =
        {
            OneToTwo
        };

        public static int Oldest => SaveCodec.CurrentSchema - steps.Length;

        public static bool CanUpgrade(int schema) => schema >= Oldest && schema <= SaveCodec.CurrentSchema;

        public static SaveGameData Upgrade(SaveGameData data)
        {
            if (data == null || !CanUpgrade(data.schemaVersion)) return data;
            while (data.schemaVersion < SaveCodec.CurrentSchema)
            {
                data = steps[data.schemaVersion - Oldest](data);
                data.schemaVersion++;
            }
            return data;
        }

        /// <summary>
        /// Schema 1 left fuel and survivor needs unset on saves written before those systems existed,
        /// and the loader guessed at them on every read. Schema 2 writes the guess into the save once.
        /// </summary>
        private static SaveGameData OneToTwo(SaveGameData data)
        {
            if (data.fuelSet == 0)
            {
                data.fuel = (int)Math.Round(FuelTank.Start * 10f);
                data.fuelSet = 1;
            }
            data.hour = WrapHour(data.hour);
            data.survivors = data.survivors ?? Array.Empty<SurvivorSave>();
            data.modules = data.modules ?? Array.Empty<ModuleSave>();
            var kept = new List<SurvivorSave>(data.survivors.Length);
            foreach (var s in data.survivors)
            {
                if (s == null) continue;
                if (!s.needsTracked)
                {
                    s.hunger = UntrackedNeed;
                    s.thirst = UntrackedNeed;
                    s.fatigue = 0f;
                    s.fatigueKnown = 0;
                    s.opinion = string.IsNullOrEmpty(s.bond) ? 0 : BondOpinion;
                    s.injury = 0;
                    s.needsTracked = true;
                }
                s.fatigue = s.fatigue < 0f ? 0f : (s.fatigue > 100f ? 100f : s.fatigue);
                s.aside = s.aside ?? "";
                s.mark = s.mark ?? "";
                s.kin = s.kin ?? "";
                s.practice = s.practice ?? "";
                s.past = s.past ?? "";
                if (string.IsNullOrEmpty(s.task)) s.task = "Rest";
                kept.Add(s);
            }
            data.survivors = kept.ToArray();
            var modules = new List<ModuleSave>(data.modules.Length);
            foreach (var m in data.modules)
            {
                if (m == null || string.IsNullOrEmpty(m.kind)) continue;
                m.integrity = m.integrity < 0 ? 0 : (m.integrity > 100 ? 100 : m.integrity);
                modules.Add(m);
            }
            data.modules = modules.ToArray();
            return data;
        }

        public static float WrapHour(float hour)
        {
            if (float.IsNaN(hour) || float.IsInfinity(hour)) return 18.5f;
            hour %= 24f;
            return hour < 0f ? hour + 24f : hour;
        }
    }

    /// <summary>Field-by-field comparison of two saves, for the round-trip deep-equality check.</summary>
    public static class SaveDiff
    {
        public static List<string> Compare(object a, object b)
        {
            var diffs = new List<string>();
            Walk("save", a, b, diffs);
            return diffs;
        }

        private static void Walk(string path, object a, object b, List<string> diffs)
        {
            if (a == null || b == null)
            {
                if (!(a == null && b == null) && !(IsEmpty(a) && IsEmpty(b))) diffs.Add(path);
                return;
            }
            var type = a.GetType();
            if (type != b.GetType())
            {
                diffs.Add(path);
                return;
            }
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type == typeof(decimal))
            {
                if (!a.Equals(b)) diffs.Add(path);
                return;
            }
            if (a is IList listA)
            {
                var listB = (IList)b;
                if (listA.Count != listB.Count)
                {
                    diffs.Add(path + ".Length");
                    return;
                }
                for (int i = 0; i < listA.Count; i++) Walk(path + "[" + i + "]", listA[i], listB[i], diffs);
                return;
            }
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                Walk(path + "." + field.Name, field.GetValue(a), field.GetValue(b), diffs);
        }

        private static bool IsEmpty(object value) =>
            value == null || (value is string s && s.Length == 0) || (value is IList l && l.Count == 0);
    }
}

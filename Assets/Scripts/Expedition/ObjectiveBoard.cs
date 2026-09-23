using System;
using System.Collections.Generic;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    public enum ObjectiveKind
    {
        Collect,
        Reach,
        Retrieve,
        Rescue,
        ClearNest
    }

    /// <summary>
    /// One expedition objective. Collect counts pickups of an item category, Reach a named spot, Retrieve a key
    /// item id or "poi" for the marked room, Rescue a survivor id brought home, ClearNest kills near a nest.
    /// An empty target matches any key of the kind.
    /// </summary>
    public readonly struct ObjectiveSpec
    {
        public readonly string Id;
        public readonly ObjectiveKind Kind;
        public readonly string Target;
        public readonly int Count;
        public readonly bool Bonus;
        public readonly int Reward;
        public readonly string Key;
        public readonly string Fallback;

        public ObjectiveSpec(string id, ObjectiveKind kind, string target, int count, bool bonus, int reward, string key, string fallback)
        {
            Id = id ?? "";
            Kind = kind;
            Target = target ?? "";
            Count = count < 1 ? 1 : count;
            Bonus = bonus;
            Reward = reward < 0 ? 0 : reward;
            Key = key ?? "";
            Fallback = fallback ?? "";
        }

        public bool Matches(ObjectiveKind kind, string key)
        {
            if (kind != Kind) return false;
            return Target.Length == 0 || string.Equals(Target, key ?? "", StringComparison.OrdinalIgnoreCase);
        }

        public string Title(string language)
        {
            if (Key.Length == 0) return Fallback;
            string line = string.IsNullOrEmpty(language) ? Loc.T(Key) : Loc.T(Key, language);
            return line == Key && Fallback.Length > 0 ? Fallback : line;
        }
    }

    /// <summary>The objectives of one expedition and their progress. Extraction waits on every required one.</summary>
    public class ObjectiveBoard
    {
        public const float ReachRadius = 3.5f;
        public const float NestRadius = 9f;

        private readonly List<ObjectiveSpec> specs = new List<ObjectiveSpec>();
        private readonly List<int> progress = new List<int>();

        public ObjectiveBoard(IEnumerable<ObjectiveSpec> list)
        {
            if (list == null) return;
            var seen = new HashSet<string>();
            foreach (var spec in list)
            {
                if (spec.Id.Length == 0 || !seen.Add(spec.Id)) continue;
                specs.Add(spec);
                progress.Add(0);
            }
        }

        public int Count => specs.Count;
        public ObjectiveSpec Spec(int index) => specs[index];
        public int Progress(int index) => progress[index];
        public bool Done(int index) => progress[index] >= specs[index].Count;

        public bool Wants(ObjectiveKind kind)
        {
            for (int i = 0; i < specs.Count; i++)
                if (specs[i].Kind == kind && !Done(i)) return true;
            return false;
        }

        /// <summary>Adds progress to every open objective the note matches and returns the ones it finished.</summary>
        public List<ObjectiveSpec> Note(ObjectiveKind kind, string key, int amount)
        {
            var finished = new List<ObjectiveSpec>();
            if (amount <= 0) return finished;
            for (int i = 0; i < specs.Count; i++)
            {
                if (Done(i) || !specs[i].Matches(kind, key)) continue;
                progress[i] = Math.Min(specs[i].Count, progress[i] + amount);
                if (Done(i)) finished.Add(specs[i]);
            }
            return finished;
        }

        /// <summary>
        /// Drops open objectives that can no longer be met this run, such as a room already searched. An empty key
        /// drops every open objective of the kind.
        /// </summary>
        public int Waive(ObjectiveKind kind, string key)
        {
            int dropped = 0;
            for (int i = specs.Count - 1; i >= 0; i--)
            {
                if (Done(i) || specs[i].Kind != kind) continue;
                if (!string.IsNullOrEmpty(key) && !specs[i].Matches(kind, key)) continue;
                specs.RemoveAt(i);
                progress.RemoveAt(i);
                dropped++;
            }
            return dropped;
        }

        public bool RequiredDone
        {
            get
            {
                for (int i = 0; i < specs.Count; i++)
                    if (!specs[i].Bonus && !Done(i)) return false;
                return true;
            }
        }

        public int DoneCount
        {
            get
            {
                int done = 0;
                for (int i = 0; i < specs.Count; i++)
                    if (Done(i)) done++;
                return done;
            }
        }

        public int Tally
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < progress.Count; i++) sum += progress[i];
                return sum;
            }
        }

        public int BonusReward
        {
            get
            {
                int reward = 0;
                for (int i = 0; i < specs.Count; i++)
                    if (Done(i)) reward += specs[i].Reward;
                return reward;
            }
        }

        public string Line(int index, string language)
        {
            var spec = specs[index];
            string mark = Done(index) ? "[x] " : "[ ] ";
            string bonus = spec.Bonus ? Text("obj.bonus", language) + ": " : "";
            string count = spec.Count > 1 ? " " + progress[index] + "/" + spec.Count : "";
            return mark + bonus + spec.Title(language) + count;
        }

        public string Lines(string language)
        {
            var lines = new List<string>();
            for (int i = 0; i < specs.Count; i++) lines.Add(Line(i, language));
            return string.Join("\n", lines);
        }

        public static string Finished(ObjectiveSpec spec, string language)
        {
            string line = Text("obj.done", language) + " " + spec.Title(language);
            return spec.Reward > 0 ? line + "  +" + spec.Reward + " " + Text("result.scrap", language) : line;
        }

        private static string Text(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Loc.T(key) : Loc.T(key, language);
        }
    }

    /// <summary>
    /// The objectives each district's expedition carries. <see cref="ExpeditionBook"/> replaces the built-in
    /// table with the authored ExpeditionDefinition assets.
    /// </summary>
    public static class ObjectivePlan
    {
        public const string Prototype = "ash_market";

        public static readonly KeyValuePair<string, ObjectiveSpec[]>[] Code =
        {
            new KeyValuePair<string, ObjectiveSpec[]>(Prototype, new[]
            {
                new ObjectiveSpec("am.medical", ObjectiveKind.Collect, "Medical", 2, true, 4, "obj.am.medical", "Find medical supplies"),
                new ObjectiveSpec("am.nest", ObjectiveKind.ClearNest, "nest", 3, true, 6, "obj.am.nest", "Clear the nest by the stalls"),
                new ObjectiveSpec("am.part", ObjectiveKind.Retrieve, "poi", 1, true, 5, "obj.am.part", "Recover the generator part from the cache"),
            }),
            new KeyValuePair<string, ObjectiveSpec[]>("old_hospital", new[]
            {
                new ObjectiveSpec("oh.rescue", ObjectiveKind.Rescue, "rescue_hospital", 1, true, 4, "obj.oh.rescue", "Bring the field medic home"),
            }),
        };

        private static Dictionary<string, ObjectiveSpec[]> table;

        public static bool FromAsset { get; private set; }

        public static ObjectiveSpec[] For(string districtId)
        {
            if (table == null) Clear();
            return table.TryGetValue(districtId ?? "", out var list) ? list : Array.Empty<ObjectiveSpec>();
        }

        public static void Use(IEnumerable<KeyValuePair<string, ObjectiveSpec[]>> expeditions)
        {
            var next = new Dictionary<string, ObjectiveSpec[]>();
            if (expeditions != null)
                foreach (var pair in expeditions)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value == null || pair.Value.Length == 0) continue;
                    next[pair.Key] = pair.Value;
                }
            if (next.Count == 0)
            {
                Clear();
                return;
            }
            table = next;
            FromAsset = true;
        }

        public static void Clear()
        {
            table = new Dictionary<string, ObjectiveSpec[]>();
            foreach (var pair in Code) table[pair.Key] = pair.Value;
            FromAsset = false;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Authored clips keyed by ClipBook id. An id with variants plays one of them;
    /// an id without any keeps its procedural tone, so the game never goes silent.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "Outpost Zero/Sfx Library")]
    public class SfxLibrary : ScriptableObject
    {
        public const string ResourcePath = "Audio/SfxLibrary";

        [Serializable]
        public class Entry
        {
            public string id;
            public AudioClip[] variants = Array.Empty<AudioClip>();
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>Picks a variant, never the one this id played last while it has another.</summary>
        public AudioClip Pick(string id, int roll, Dictionary<string, int> last)
        {
            var entry = Find(id);
            if (entry == null) return null;
            int previous = last != null && last.TryGetValue(id, out int seen) ? seen : -1;
            int slot = Fresh(entry.variants.Length, previous, roll);
            if (slot < 0) return null;
            if (last != null) last[id] = slot;
            return entry.variants[slot];
        }

        private Entry Find(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            foreach (var entry in entries)
                if (entry != null && entry.id == id && entry.variants != null) return entry;
            return null;
        }

        public static int Fresh(int count, int previous, int roll)
        {
            int slot = Variant(count, roll);
            if (count > 1 && slot == previous) slot = (slot + 1) % count;
            return slot;
        }

        public AudioClip Pick(string id, int roll)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.id != id || entry.variants == null) continue;
                int slot = Variant(entry.variants.Length, roll);
                return slot >= 0 ? entry.variants[slot] : null;
            }
            return null;
        }

        public string[] Ids()
        {
            var ids = new List<string>();
            if (entries == null) return ids.ToArray();
            foreach (var entry in entries)
                if (entry != null) ids.Add(entry.id);
            return ids.ToArray();
        }

        public static int Variant(int count, int roll)
        {
            if (count <= 0) return -1;
            int slot = roll % count;
            return slot < 0 ? slot + count : slot;
        }

        /// <summary>
        /// Ids a library names that the game never plays, plus ids named twice.
        /// </summary>
        public static string[] Stray(string[] ids)
        {
            var stray = new List<string>();
            var seen = new HashSet<string>();
            if (ids == null) return stray.ToArray();
            foreach (var id in ids)
            {
                if (!ClipBook.Has(id) || !seen.Add(id)) stray.Add(id ?? "");
            }
            return stray.ToArray();
        }
    }
}

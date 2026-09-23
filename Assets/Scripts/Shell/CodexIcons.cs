using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Model id to its rendered icon, for every manifest entry tagged "codex".
    /// <c>BlenderScripts/codex_icons.py --fix</c> rebuilds it from the manifest.
    /// </summary>
    [CreateAssetMenu(fileName = "CodexIcons", menuName = "Outpost Zero/Codex Icons")]
    public class CodexIcons : ScriptableObject
    {
        public const string ResourcePath = "CodexIcons";

        [Serializable]
        public class Entry
        {
            public string id;
            public Texture2D icon;
        }

        public List<Entry> entries = new List<Entry>();

        private static CodexIcons loaded;
        private static bool tried;

        public Texture2D Find(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == id) return entries[i].icon;
            }
            return null;
        }

        public static Texture2D For(CodexBook.Entry entry)
        {
            if (entry == null) return null;
            string item = CodexBook.ItemOf(entry.Id);
            if (item.Length > 0) return Items.ItemDatabase.Icon(item);
            if (string.IsNullOrEmpty(entry.Model)) return null;
            if (!tried)
            {
                tried = true;
                loaded = Resources.Load<CodexIcons>(ResourcePath);
            }
            return loaded != null ? loaded.Find(entry.Model) : null;
        }
    }
}

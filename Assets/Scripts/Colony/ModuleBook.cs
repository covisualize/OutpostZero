using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>The module list the yard loads from Resources. Tools > Outpost Zero > Sync Module Book rebuilds it.</summary>
    public class ModuleBook : ScriptableObject
    {
        public const string ResourcePath = "ModuleBook";

        public BuildingModuleDefinition[] modules = new BuildingModuleDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<ModuleBook>(ResourcePath));
        }

        public static void Use(ModuleBook book)
        {
            var rows = new List<ModuleTable.Row>();
            if (book != null && book.modules != null)
            {
                foreach (var module in book.modules)
                    if (module != null && !string.IsNullOrEmpty(module.id)) rows.Add(module.ToRow());
            }
            ModuleTable.Use(rows);
        }
    }
}

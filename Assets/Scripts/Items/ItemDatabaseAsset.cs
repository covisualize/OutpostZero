using UnityEngine;

namespace OutpostZero.Items
{
    /// <summary>The list <see cref="ItemDatabase"/> loads from Resources. Tools > Outpost Zero > Sync Item Database rebuilds it.</summary>
    public class ItemDatabaseAsset : ScriptableObject
    {
        public ItemDefinition[] items = new ItemDefinition[0];
        public LootTableDefinition[] tables = new LootTableDefinition[0];
    }
}

using UnityEngine;

namespace OutpostZero.Items
{
    /// <summary>Designer-facing loot table. Entries roll in order, so reordering changes which crates hold what.</summary>
    [CreateAssetMenu(fileName = "LootTable", menuName = "Outpost Zero/Loot Table")]
    public class LootTableDefinition : ScriptableObject
    {
        [Tooltip("Table id a container asks for: crate, medical, military, or a district's own table.")]
        public string id = LootTables.Crate;
        public LootEntry[] entries = new LootEntry[0];
    }
}

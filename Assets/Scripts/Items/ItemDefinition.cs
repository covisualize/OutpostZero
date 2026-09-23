using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Items
{
    /// <summary>
    /// Designer-facing data for one pack item. <see cref="ItemDatabase"/> loads every definition at startup
    /// and pushes its stats into <see cref="ItemCatalog"/>, so gameplay reads the tuned values.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Outpost Zero/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves, loot tables, recipes and the string table.")]
        public string id = "item";
        [Tooltip("English name. The pack shows the localized name from the string table.")]
        public string displayName = "Item";
        [Tooltip("Pack section and belt rules.")]
        public ItemCategory category = ItemCategory.KeyItem;
        [Tooltip("Kilograms per unit, counted against the pack's carry limit.")]
        public float weight = 0.1f;
        [Tooltip("What using the item does.")]
        public ItemUse use = ItemUse.None;
        [Tooltip("Conditions using it treats: stop bleeding, cure infection (stages I and II), pain relief (20 health over 20 s).")]
        public UseEffect useEffect = UseEffect.None;
        [Tooltip("Health restored on use, in hit points.")]
        public int heal;
        [Tooltip("Hunger restored on use, out of 100.")]
        public float hunger;
        [Tooltip("Thirst restored on use, out of 100.")]
        public float thirst;
        [Tooltip("Manifest id of the generated model shown in the world and rendered as the icon.")]
        public string worldModel = "";
        [Tooltip("Prefab spawned when the item is dropped or placed as loot. Filled in by Tools > Outpost Zero > Sync Item Database.")]
        public GameObject worldPrefab;
        [Tooltip("Pack icon rendered from the world model by the Blender pipeline.")]
        public Texture2D icon;

        public virtual ItemRecord ToRecord() => new ItemRecord
        {
            Id = id,
            DisplayName = displayName,
            Category = category,
            Weight = weight,
            Use = use,
            Effect = useEffect,
            Heal = heal,
            Hunger = hunger,
            Thirst = thirst
        };

        public virtual void CopyFrom(ItemRecord record)
        {
            if (record == null) return;
            id = record.Id;
            displayName = record.DisplayName;
            category = record.Category;
            weight = record.Weight;
            use = record.Use;
            useEffect = record.Effect;
            heal = record.Heal;
            hunger = record.Hunger;
            thirst = record.Thirst;
            worldModel = ItemVisuals.ModelFor(record.Id);
        }
    }
}

using System.Collections.Generic;

namespace OutpostZero.Items
{
    /// <summary>
    /// Which generated model each item shows in the world and in the pack.
    /// The model id is the Blender manifest id, so its prefab and rendered icon follow the pipeline's paths.
    /// </summary>
    public static class ItemVisuals
    {
        public const string Folder = "Weapons";

        private static readonly Dictionary<string, string> models = new Dictionary<string, string>
        {
            ["medkit"] = "Loot_Medkit",
            ["bandage"] = "Loot_Bandage",
            ["antibiotics"] = "Loot_Antibiotics",
            ["painkillers"] = "Loot_Painkillers",
            ["canned_food"] = "Loot_CannedFood",
            ["raw_food"] = "Loot_RawFood",
            ["water"] = "Loot_WaterBottle",
            ["ammo_9mm"] = "Loot_AmmoBox_9mm",
            ["ammo_shells"] = "Loot_AmmoBox_Shotgun",
            ["ammo_rifle"] = "Loot_AmmoBox_Rifle",
            ["ammo_smg"] = "Loot_AmmoBox_SMG",
            ["scrap"] = "Loot_ScrapPile",
            ["cloth"] = "Loot_Cloth",
            ["chemicals"] = "Loot_Chemicals",
            ["tape"] = "Loot_DuctTape",
            ["noise_lure"] = "Loot_NoiseLure",
            ["street_bottle"] = "Loot_Bottle",
            ["molotov"] = "Loot_Molotov",
            ["flare"] = "Loot_Flare",
            ["pipe_bomb"] = "Loot_PipeBomb",
            ["print_flare"] = "Loot_Blueprint",
            ["print_repair"] = "Loot_Blueprint",
            ["print_wall"] = "Loot_Blueprint",
            ["print_radio"] = "Loot_Blueprint",
            ["cell"] = "Loot_LampCell",
            ["generator_part"] = "Loot_GeneratorPart"
        };

        public static IEnumerable<string> ItemIds => models.Keys;

        public static string ModelFor(string itemId)
        {
            if (itemId != null && models.TryGetValue(itemId, out var model)) return model;
            return "";
        }

        public static string ModelPath(string modelId) => "Assets/Models/" + Folder + "/" + modelId + ".fbx";

        public static string IconPath(string modelId) => "Assets/Models/" + Folder + "/" + modelId + "_Icon.png";

        public static string PrefabPath(string modelId) => "Assets/Prefabs/" + Folder + "/" + modelId + ".prefab";

        public static string AssetName(string itemId) => itemId;

        public static string AssetPath(string itemId) => "Assets/Data/Items/" + AssetName(itemId) + ".asset";
    }
}

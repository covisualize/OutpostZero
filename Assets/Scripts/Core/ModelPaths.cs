namespace OutpostZero.Core
{
    /// <summary>
    /// Canonical FBX paths under Assets/Models. Scene builder and EditMode tests share this list
    /// so a renamed artifact fails CI instead of becoming a placeholder cube.
    /// </summary>
    public static class ModelPaths
    {
        public const string Root = "Assets/Models";

        public const string StreetLamp = Root + "/Props/Prop_StreetLamp.fbx";
        public const string SurvivorLeader = Root + "/Characters/Survivor_Leader.fbx";
        public const string ZombieWalker = Root + "/Characters/Zombie_Walker.fbx";
        public const string ZombieRunner = Root + "/Characters/Zombie_Runner.fbx";
        public const string ZombieBrute = Root + "/Characters/Zombie_Brute.fbx";
        public const string Merchant = Root + "/Characters/NPC_Merchant.fbx";
        public const string Colonist = Root + "/Characters/Colonist_Survivor.fbx";

        public const string Pistol = Root + "/Weapons/Weapon_Pistol_9mm.fbx";
        public const string Shotgun = Root + "/Weapons/Weapon_Shotgun_Pump.fbx";
        public const string Machete = Root + "/Weapons/Weapon_Machete.fbx";
        public const string AssaultRifle = Root + "/Weapons/Weapon_AssaultRifle.fbx";
        public const string Ammo9mm = Root + "/Weapons/Loot_AmmoBox_9mm.fbx";
        public const string AmmoShotgun = Root + "/Weapons/Loot_AmmoBox_Shotgun.fbx";
        public const string Medkit = Root + "/Weapons/Loot_Medkit.fbx";
        public const string ScrapPile = Root + "/Weapons/Loot_ScrapPile.fbx";

        public const string RoadStraight = Root + "/Environment/Road_Tile_Straight.fbx";
        public const string RoadIntersection = Root + "/Environment/Road_Tile_Intersection.fbx";
        public const string Storefront = Root + "/Environment/Building_Storefront_2Story.fbx";
        public const string Warehouse = Root + "/Environment/Building_Warehouse_Depot.fbx";
        public const string RuinCorner = Root + "/Environment/Ruin_Wall_Corner.fbx";

        public const string JerseyBarrier = Root + "/Props/Barricade_Concrete_Jersey.fbx";
        public const string WoodWire = Root + "/Props/Barricade_Wood_Wire.fbx";
        public const string Sandbags = Root + "/Props/Barricade_Sandbags.fbx";
        public const string Sedan = Root + "/Props/Vehicle_Wrecked_Sedan.fbx";
        public const string Truck = Root + "/Props/Vehicle_Apocalypse_Truck.fbx";
        public const string Dumpster = Root + "/Props/Prop_Dumpster.fbx";
        public const string BarrelExplosive = Root + "/Props/Prop_Barrel_Red_Explosive.fbx";
        public const string BarrelToxic = Root + "/Props/Prop_Barrel_Toxic.fbx";
        public const string BarrelOil = Root + "/Props/Prop_Barrel_Oil.fbx";
        public const string CrateWood = Root + "/Props/Prop_Crate_Wood.fbx";
        public const string CrateMilitary = Root + "/Props/Prop_Crate_Military.fbx";
        public const string StreetBench = Root + "/Props/Prop_StreetBench.fbx";

        public const string Workbench = Root + "/BaseBuilding/Base_CraftingWorkbench.fbx";
        public const string Campfire = Root + "/BaseBuilding/Base_Campfire_Cooker.fbx";
        public const string Generator = Root + "/BaseBuilding/Base_Generator_Diesel.fbx";
        public const string MedicalCot = Root + "/BaseBuilding/Base_MedicalCot.fbx";
        public const string Watchtower = Root + "/BaseBuilding/Base_Watchtower.fbx";
        public const string WaterCollector = Root + "/BaseBuilding/Base_WaterCollector.fbx";
        public const string HydroponicFarm = Root + "/BaseBuilding/Base_HydroponicFarm.fbx";
        public const string AutoTurret = Root + "/BaseBuilding/Base_AutoTurret.fbx";

        public static readonly string[] All =
        {
            StreetLamp, SurvivorLeader, ZombieWalker, ZombieRunner, ZombieBrute, Merchant, Colonist,
            Pistol, Shotgun, Machete, AssaultRifle, Ammo9mm, AmmoShotgun, Medkit, ScrapPile,
            RoadStraight, RoadIntersection, Storefront, Warehouse, RuinCorner,
            JerseyBarrier, WoodWire, Sandbags, Sedan, Truck, Dumpster,
            BarrelExplosive, BarrelToxic, BarrelOil, CrateWood, CrateMilitary, StreetBench,
            Workbench, Campfire, Generator, MedicalCot, Watchtower, WaterCollector, HydroponicFarm, AutoTurret
        };

        public static string Relative(string assetPath)
        {
            const string prefix = Root + "/";
            return assetPath.StartsWith(prefix) ? assetPath.Substring(prefix.Length) : assetPath;
        }
    }
}

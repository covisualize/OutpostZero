namespace OutpostZero.Core
{
    public enum GameState
    {
        CampManagement,
        ExpeditionActive,
        Paused,
        GameOver,
        SuccessionScreen,
        ExpeditionResults,
        RaidActive,
        MainMenu,
        Victory
    }

    public enum NoiseType
    {
        SneakFootstep,
        WalkFootstep,
        SprintFootstep,
        MeleeSwing,
        GunshotQuiet,      // Suppressed / Small caliber
        GunshotLoud,       // Rifle / Shotgun
        Explosion,
        ObjectBroken,
        ZombieScream,
        Thunder,
        BleedDrip
    }

    public enum WeaponType
    {
        Melee,
        Pistol,
        Shotgun,
        Rifle,
        SMG
    }

    public enum ItemCategory
    {
        Weapon,
        Ammunition,
        Medical,
        FoodWater,
        ScrapMaterial,
        Fuel,
        KeyItem
    }
}

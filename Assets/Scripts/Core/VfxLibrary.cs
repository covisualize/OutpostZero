using System;
using UnityEngine;

namespace OutpostZero.Core
{
    /// <summary>Every effect the game can play. Weapons, archetypes and hazards name theirs by event.</summary>
    public enum VfxEvent
    {
        None = 0,
        MuzzlePistol = 1,
        MuzzleShotgun = 2,
        MuzzleRifle = 3,
        MuzzleSmoke = 4,
        Tracer = 5,
        Shell = 6,
        BrassMark = 7,
        ImpactSpark = 8,
        ImpactDust = 9,
        ImpactSplinter = 10,
        BloodSpray = 11,
        BloodMist = 12,
        BloodDrip = 13,
        DeathBurst = 14,
        ExplosionFlash = 15,
        Fireball = 16,
        Shockwave = 17,
        Debris = 18,
        SmokeColumn = 19,
        GroundFire = 20,
        ToxicCloud = 21,
        OilFire = 22,
        FootDust = 23,
        FootSplash = 24,
        Embers = 25,
        Flies = 26,
        LampSparks = 27,
        Lightning = 28,
    }

    /// <summary>
    /// Maps each <see cref="VfxEvent"/> to an authored prefab and its pool settings. An entry
    /// with no prefab plays the built-in procedural effect, so the game runs with an empty library.
    /// </summary>
    [CreateAssetMenu(menuName = "Outpost Zero/VFX Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        public const string ResourcePath = "VfxLibrary";

        [Serializable]
        public class Entry
        {
            [Tooltip("The event this entry plays.")]
            public VfxEvent id;
            [Tooltip("Authored effect; leave empty to use the built-in procedural one.")]
            public GameObject prefab;
            [Tooltip("Instances made when the pool first loads.")]
            public int prewarm;
            [Tooltip("Idle instances the pool keeps; 0 uses the event's default.")]
            public int keep;
            [Tooltip("Seconds before the instance returns to the pool; 0 uses the event's default.")]
            public float life;
        }

        public Entry[] entries = new Entry[0];

        private static VfxLibrary loaded;
        private static bool tried;

        public static VfxLibrary Active
        {
            get
            {
                if (!tried)
                {
                    tried = true;
                    loaded = Resources.Load<VfxLibrary>(ResourcePath);
                }
                return loaded;
            }
        }

        public Entry Find(VfxEvent id)
        {
            return FindIn(entries, id);
        }

        public static Entry FindIn(Entry[] list, VfxEvent id)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Length; i++)
                if (list[i] != null && list[i].id == id) return list[i];
            return null;
        }
    }

    /// <summary>Default lifetimes, pool sizes and the event each source plays.</summary>
    public static class VfxBook
    {
        public static float Life(VfxEvent id)
        {
            switch (id)
            {
                case VfxEvent.MuzzlePistol:
                case VfxEvent.MuzzleShotgun:
                case VfxEvent.MuzzleRifle: return 0.12f;
                case VfxEvent.Tracer: return 0.05f;
                case VfxEvent.MuzzleSmoke: return 0.4f;
                case VfxEvent.Shell: return 1.4f;
                case VfxEvent.BrassMark: return 6f;
                case VfxEvent.ImpactSpark:
                case VfxEvent.ImpactDust:
                case VfxEvent.ImpactSplinter:
                case VfxEvent.BloodSpray: return 0.6f;
                case VfxEvent.BloodMist: return 0.6f;
                case VfxEvent.BloodDrip: return 6f;
                case VfxEvent.DeathBurst: return 0.9f;
                case VfxEvent.ExplosionFlash: return 0.35f;
                case VfxEvent.Shockwave: return 0.45f;
                case VfxEvent.SmokeColumn: return 6f;
                case VfxEvent.FootDust:
                case VfxEvent.FootSplash: return 0.6f;
                case VfxEvent.Embers: return 1.2f;
                case VfxEvent.LampSparks: return 0.4f;
                case VfxEvent.Flies: return 4f;
                case VfxEvent.Lightning: return 0.1f;
                default: return 1f;
            }
        }

        /// <summary>Idle instances kept for reuse: the busiest effects keep the most.</summary>
        public static int Keep(VfxEvent id)
        {
            switch (id)
            {
                case VfxEvent.MuzzlePistol:
                case VfxEvent.MuzzleShotgun:
                case VfxEvent.MuzzleRifle:
                case VfxEvent.Tracer:
                case VfxEvent.Shell: return 24;
                case VfxEvent.BrassMark: return 40;
                case VfxEvent.ImpactSpark:
                case VfxEvent.ImpactDust:
                case VfxEvent.ImpactSplinter:
                case VfxEvent.BloodSpray: return 16;
                case VfxEvent.FootDust:
                case VfxEvent.FootSplash: return 8;
                default: return 4;
            }
        }

        public static VfxEvent MuzzleFor(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Shotgun: return VfxEvent.MuzzleShotgun;
                case WeaponType.Rifle:
                case WeaponType.SMG: return VfxEvent.MuzzleRifle;
                case WeaponType.Melee: return VfxEvent.None;
                default: return VfxEvent.MuzzlePistol;
            }
        }

        /// <summary>The burst a hit throws for a StrikeFace surface family.</summary>
        public static VfxEvent ImpactFor(string face)
        {
            if (face == "flesh") return VfxEvent.BloodSpray;
            if (face == "metal") return VfxEvent.ImpactSpark;
            if (face == "wood") return VfxEvent.ImpactSplinter;
            return VfxEvent.ImpactDust;
        }

        public static bool Keeps(int idle, int keep)
        {
            return idle < (keep > 0 ? keep : 1);
        }
    }

    /// <summary>Pool counters for the watch page: a leak shows as live climbing while nothing plays.</summary>
    public static class VfxStats
    {
        public static int Created { get; private set; }
        public static int Reused { get; private set; }
        public static int Live { get; private set; }
        public static int Idle { get; private set; }
        public static int Dropped { get; private set; }

        public static void Reset()
        {
            Created = 0;
            Reused = 0;
            Live = 0;
            Idle = 0;
            Dropped = 0;
        }

        public static void Make()
        {
            Created++;
            Live++;
        }

        public static void Reuse()
        {
            Reused++;
            Live++;
            if (Idle > 0) Idle--;
        }

        public static void Park()
        {
            if (Live > 0) Live--;
            Idle++;
        }

        public static void Drop()
        {
            if (Live > 0) Live--;
            Dropped++;
        }

        public static void Forget()
        {
            if (Idle > 0) Idle--;
        }

        public static int ReusePercent()
        {
            int total = Created + Reused;
            return total <= 0 ? 0 : (int)(Reused * 100L / total);
        }
    }
}

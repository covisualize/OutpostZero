using System.Collections.Generic;

namespace OutpostZero.Sensory
{
    /// <summary>
    /// How far each world noise carries and how loud it lands, by source id. Empty until <see cref="NoiseBook.Ensure"/>
    /// fills it from Resources/NoiseBook; until then, and for any id the book lacks, the built-in rows answer.
    /// Weapons, melee, throwables and zombie screams carry their noise on their own definitions.
    /// </summary>
    public static class NoiseTable
    {
        public sealed class Row
        {
            public string Id = "";
            /// <summary>Metres the noise reaches.</summary>
            public float Radius;
            /// <summary>0 to 1: how strongly a zombie inside the reach reacts.</summary>
            public float Loud;
        }

        public const string StepWalk = "step_walk";
        public const string StepSprint = "step_sprint";
        public const string StepCrouch = "step_crouch";
        public const string Cough = "cough";
        public const string Ladder = "ladder";
        public const string LootTake = "loot_take";
        public const string Ration = "ration";
        public const string Takedown = "takedown";
        public const string Bleed = "bleed";
        public const string Shell = "shell";
        public const string DoorCreak = "door_creak";
        public const string DoorBash = "door_bash";
        public const string DoorBreak = "door_break";
        public const string Glass = "glass";
        public const string RescueCall = "rescue_call";
        public const string StreetAid = "street_aid";
        public const string BarrelBlast = "barrel_blast";
        public const string BarrelBurst = "barrel_burst";
        public const string Thunder = "thunder";
        public const string RaidTurret = "raid_turret";
        public const string RaidGuard = "raid_guard";

        private static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        private static List<Row> builtIn;

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            rows.Clear();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || string.IsNullOrEmpty(row.Id) || rows.ContainsKey(row.Id)) continue;
                rows[row.Id] = row;
            }
        }

        public static void Clear()
        {
            rows.Clear();
        }

        /// <summary>True when the loaded book tunes this id, not just the built-in row.</summary>
        public static bool Has(string id)
        {
            return !string.IsNullOrEmpty(id) && rows.ContainsKey(id);
        }

        public static Row Of(string id)
        {
            if (!string.IsNullOrEmpty(id) && rows.TryGetValue(id, out var row)) return row;
            var list = BuiltInRows();
            for (int i = 0; i < list.Count; i++) if (list[i].Id == id) return list[i];
            return new Row { Id = id ?? "" };
        }

        public static float Radius(string id)
        {
            float radius = Of(id).Radius;
            return radius > 0f ? radius : 0f;
        }

        public static float Loud(string id)
        {
            float loud = Of(id).Loud;
            if (loud < 0f) return 0f;
            return loud > 1f ? 1f : loud;
        }

        /// <summary>The code values as rows: what Sync Noise Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            if (builtIn != null) return builtIn;
            builtIn = new List<Row>
            {
                new Row { Id = StepWalk, Radius = 6f, Loud = 0.7f },
                new Row { Id = StepSprint, Radius = 13f, Loud = 0.7f },
                new Row { Id = StepCrouch, Radius = 2f, Loud = 0.7f },
                new Row { Id = Cough, Radius = Graphics.AshCough.Radius, Loud = 0.7f },
                new Row { Id = Ladder, Radius = Expedition.LadderGrip.Noise, Loud = 0.35f },
                new Row { Id = LootTake, Radius = Player.LootTake.Noise, Loud = 0.4f },
                new Row { Id = Ration, Radius = Player.RationNoise.Radius, Loud = Player.RationNoise.Loud },
                new Row { Id = Takedown, Radius = Player.QuietKill.Noise, Loud = 0.45f },
                new Row { Id = Bleed, Radius = BleedScent.Radius, Loud = BleedScent.Loud },
                new Row { Id = Shell, Radius = Combat.ShellRing.Radius, Loud = Combat.ShellRing.Loud },
                new Row { Id = DoorCreak, Radius = Expedition.DoorCreak.Radius, Loud = Expedition.DoorCreak.Loud },
                new Row { Id = DoorBash, Radius = Expedition.DoorBar.Noise, Loud = 0.85f },
                new Row { Id = DoorBreak, Radius = Expedition.DoorBar.BreakNoise, Loud = 0.85f },
                new Row { Id = Glass, Radius = Expedition.PaneGlass.Noise, Loud = 0.8f },
                new Row { Id = RescueCall, Radius = Expedition.StraggleCall.Radius, Loud = 0.8f },
                new Row { Id = StreetAid, Radius = Expedition.StreetAid.Noise, Loud = 0.7f },
                new Row { Id = BarrelBlast, Radius = Combat.BarrelBlast.Noise, Loud = 1f },
                new Row { Id = BarrelBurst, Radius = 10f, Loud = 1f },
                new Row { Id = Thunder, Radius = StormCover.Radius, Loud = 1f },
                new Row { Id = RaidTurret, Radius = 18f, Loud = 0.7f },
                new Row { Id = RaidGuard, Radius = 14f, Loud = 0.45f }
            };
            return builtIn;
        }
    }
}

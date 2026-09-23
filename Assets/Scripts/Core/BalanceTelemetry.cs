using System;
using System.IO;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Expedition;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Core
{
    /// <summary>
    /// Appends balance rows to persistentDataPath/Telemetry/expeditions.csv and days.csv. It never leaves
    /// the machine. On in the editor and development builds; a release player needs <c>-telemetry</c>.
    /// Summarise with <c>python Tools/Balance/summarize.py</c>.
    /// </summary>
    public static class BalanceTelemetry
    {
        private static int ammoStart;
        private static int itemsStart;
        private static int expeditions;
        private static bool enabled;
        private static string folder;

        public static bool Enabled => enabled;
        public static string Folder => folder ?? "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            enabled = Application.isEditor || Debug.isDebugBuild || Array.IndexOf(Environment.GetCommandLineArgs(), "-telemetry") >= 0;
            if (!enabled) return;
            folder = Path.Combine(Application.persistentDataPath, "Telemetry");
            WorldClock.DayTurned -= OnDayTurned;
            WorldClock.DayTurned += OnDayTurned;
        }

        public static void ExpeditionStarted()
        {
            if (!enabled) return;
            ammoStart = Ammo();
            itemsStart = Items();
        }

        public static void ExpeditionEnded(ExpeditionContext context, ExpeditionOutcome outcome)
        {
            if (!enabled) return;
            expeditions++;
            var player = PlayerRegistry.Current;
            var needs = player != null ? player.GetComponent<SurvivalNeeds>() : null;
            var effects = player != null ? player.GetComponent<StatusEffectController>() : null;
            var health = player != null ? player.GetComponent<HealthSystem>() : null;
            Count(out int alive, out int deaths, out _);
            Append(BalanceLog.ExpeditionFile, BalanceLog.ExpeditionHeader, BalanceLog.Line(new BalanceLog.ExpeditionRow
            {
                Run = Run(),
                Day = context.day,
                District = context.district,
                Difficulty = context.difficulty,
                Weather = context.weather.ToString(),
                Leader = context.leaderId,
                End = outcome.end.ToString(),
                Kills = outcome.kills,
                KillGoal = outcome.killGoal,
                Scrap = outcome.scrap,
                ScrapGoal = outcome.scrapGoal,
                Seconds = outcome.seconds,
                AmmoStart = ammoStart,
                AmmoEnd = Ammo(),
                ItemsStart = itemsStart,
                ItemsEnd = Items(),
                Health = health != null ? health.CurrentHealth : 0f,
                Hunger = needs != null ? needs.Hunger : 0f,
                Thirst = needs != null ? needs.Thirst : 0f,
                Fatigue = needs != null ? needs.Fatigue : 0f,
                Infection = effects != null ? effects.Infection : 0f,
                Alive = alive,
                Deaths = deaths,
            }));
        }

        private static void OnDayTurned(int from, int to)
        {
            var storage = ColonyStorage.Instance;
            Count(out int alive, out int deaths, out float morale);
            Append(BalanceLog.DayFile, BalanceLog.DayHeader, BalanceLog.Line(new BalanceLog.DayRow
            {
                Run = Run(),
                Day = from,
                Alive = alive,
                Deaths = deaths,
                Morale = morale,
                Food = storage != null ? storage.Food : 0,
                Water = storage != null ? storage.Water : 0,
                Scrap = storage != null ? storage.Scrap : 0,
                Shots = storage != null ? storage.Shots : 0,
                Expeditions = expeditions,
                Kills = GameManager.Instance != null ? GameManager.Instance.LifetimeKills : 0,
            }));
            expeditions = 0;
        }

        private static string Run()
        {
            var map = WorldMapService.Instance;
            return BalanceLog.RunId(map != null ? map.WorldSeed : DistrictGenerator.DefaultSeed, map != null ? map.Difficulty : 2);
        }

        private static void Count(out int alive, out int deaths, out float morale)
        {
            alive = 0;
            deaths = 0;
            morale = 0f;
            var roster = SurvivorRoster.Instance;
            if (roster == null) return;
            foreach (var survivor in roster.Survivors)
            {
                if (!survivor.alive) continue;
                alive++;
                morale += survivor.morale;
            }
            if (alive > 0) morale /= alive;
            deaths = roster.Memorials.Count;
        }

        private static int Ammo()
        {
            var player = PlayerRegistry.Current;
            if (player == null) return 0;
            int total = 0;
            foreach (var gun in player.GetComponentsInChildren<FirearmWeapon>(true)) total += gun.CurrentAmmo + gun.ReserveAmmo;
            return total;
        }

        private static int Items()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (inventory == null) return 0;
            int total = 0;
            foreach (var item in inventory.Items) total += item.Quantity;
            return total;
        }

        private static void Append(string file, string header, string line)
        {
            try
            {
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, file);
                bool fresh = !File.Exists(path) || new FileInfo(path).Length == 0;
                File.AppendAllText(path, (fresh ? header + "\n" : "") + line + "\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[BalanceTelemetry] " + file + ": " + exception.Message);
            }
        }
    }
}

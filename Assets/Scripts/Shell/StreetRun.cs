using System.Collections.Generic;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Expedition;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The street_run save part. Capture writes a Merciful street run; restore only holds it, since the rest of the save
    /// (the district, the kit, the camp) has to be back before <see cref="Resume"/> reopens the street.
    /// </summary>
    public class StreetRun : MonoBehaviour, ISaveable
    {
        public static StreetRun Instance { get; private set; }

        private string pending = "";

        public string SaveId => StreetSnapshot.SaveId;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable() => SaveRegistry.Register(this);

        private void OnDisable() => SaveRegistry.Unregister(this);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string CaptureState()
        {
            var game = GameManager.Instance;
            var player = PlayerRegistry.Current;
            if (game == null || player == null) return "";
            var health = player.GetComponent<HealthSystem>();
            bool alive = health != null && !health.IsDead;
            bool merciful = SettingsService.Instance != null && SettingsService.Instance.Merciful;
            if (!StreetSnapshot.Keeps(merciful, game.CurrentState, game.ResumeState, alive)) return "";
            var run = new StreetSnapshot.Run
            {
                Timer = game.ExpeditionTime,
                Kills = game.ZombiesKilled,
                Scrap = game.ScrapLooted,
                X = player.transform.position.x,
                Z = player.transform.position.z,
                Yaw = player.transform.eulerAngles.y,
                Health = health.CurrentHealth,
                Board = ObjectiveTracker.Instance != null ? ObjectiveTracker.Instance.PackBoard() : "",
                Bodies = Bodies()
            };
            return StreetSnapshot.Pack(run);
        }

        public void RestoreState(string state)
        {
            pending = state ?? "";
        }

        /// <summary>Reopens the held street run, once, after the rest of the save has been applied.</summary>
        public bool Resume()
        {
            string held = pending;
            pending = "";
            if (!StreetSnapshot.TryUnpack(held, out var run) || GameManager.Instance == null) return false;
            return GameManager.Instance.ResumeExpedition(run);
        }

        private static List<StreetSnapshot.Body> Bodies()
        {
            var bodies = new List<StreetSnapshot.Body>();
            var spawner = FindFirstObjectByType<ZombieSpawner>();
            if (spawner == null) return bodies;
            foreach (var zombie in spawner.Active)
            {
                if (zombie == null || !zombie.activeInHierarchy) continue;
                var health = zombie.GetComponent<HealthSystem>();
                if (health == null || health.IsDead) continue;
                var marker = zombie.GetComponent<PooledZombie>();
                string prefab = marker != null && marker.SourcePrefab != null ? marker.SourcePrefab.name : "";
                bodies.Add(new StreetSnapshot.Body
                {
                    Variant = StreetSnapshot.Variant(prefab, zombie.name),
                    X = zombie.transform.position.x,
                    Z = zombie.transform.position.z,
                    Health = health.CurrentHealth
                });
            }
            return bodies;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Expedition
{
    public class ObjectiveTracker : MonoBehaviour
    {
        public static ObjectiveTracker Instance { get; private set; }

        [SerializeField] private int killGoal = 8;
        [SerializeField] private int scrapGoal = 15;
        [SerializeField] private int kills;
        [SerializeField] private int scrap;
        [SerializeField] private bool extracted;
        [SerializeField] private string poiRole = "";
        [SerializeField] private bool poiFound;

        private ObjectiveBoard board;
        private readonly List<KeyValuePair<string, Vector3>> spots = new List<KeyValuePair<string, Vector3>>();
        private float reachTimer;

        public int KillGoal => killGoal;
        public int ScrapGoal => scrapGoal;
        public int Kills => kills;
        public int Scrap => scrap;
        public bool Extracted => extracted;
        public bool ReadyToExtract => kills >= killGoal && scrap >= scrapGoal && (board == null || board.RequiredDone);
        public ObjectiveBoard Board => board;
        public string PoiRole => poiRole;
        public bool PoiFound => poiFound;

        public string PoiLine()
        {
            return LineFor(poiRole, poiFound, null);
        }

        public static string LineFor(string role, bool found)
        {
            return LineFor(role, found, "en");
        }

        public static string LineFor(string role, bool found, string language)
        {
            if (string.IsNullOrEmpty(role)) return "";
            bool radio = role == "radio";
            string key = found
                ? (radio ? "ask.stowed" : "ask.searched")
                : (radio ? "ask.findradio" : "ask.findcache");
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
        public event Action OnObjectivesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZombiesKilledChanged += OnKills;
                GameManager.Instance.OnScrapLootedChanged += OnScrap;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZombiesKilledChanged -= OnKills;
                GameManager.Instance.OnScrapLootedChanged -= OnScrap;
                GameManager.Instance.OnZombiesKilledChanged += OnKills;
                GameManager.Instance.OnScrapLootedChanged += OnScrap;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnZombiesKilledChanged -= OnKills;
                GameManager.Instance.OnScrapLootedChanged -= OnScrap;
            }
        }

        private void OnKills(int value)
        {
            kills = value;
            OnObjectivesChanged?.Invoke();
        }

        private void OnScrap(int value)
        {
            scrap = value;
            OnObjectivesChanged?.Invoke();
        }

        public void MarkExtracted()
        {
            extracted = true;
            OnObjectivesChanged?.Invoke();
        }

        public void SetGoals(int killsRequired, int scrapRequired)
        {
            killGoal = Mathf.Max(1, killsRequired);
            scrapGoal = Mathf.Max(1, scrapRequired);
            OnObjectivesChanged?.Invoke();
        }

        public void ExpectPoi(string role)
        {
            poiRole = string.IsNullOrEmpty(role) ? "" : role;
            poiFound = false;
            OnObjectivesChanged?.Invoke();
        }

        public void MarkPoi()
        {
            poiFound = true;
            OnObjectivesChanged?.Invoke();
            Note(ObjectiveKind.Retrieve, "poi", 1);
        }

        /// <summary>Loads the district's objectives. The district dressing then marks the spots they point at.</summary>
        public void Brief(string districtId)
        {
            ExpeditionBook.Ensure();
            board = new ObjectiveBoard(ObjectivePlan.For(districtId));
            spots.Clear();
            OnObjectivesChanged?.Invoke();
        }

        /// <summary>The item an open Retrieve objective sends the leader to the marked room for, or "".</summary>
        public string RoomItem()
        {
            if (board == null) return "";
            foreach (string target in board.OpenTargets(ObjectiveKind.Retrieve))
                if (OutpostZero.Items.ItemCatalog.Find(target) != null) return target;
            return "";
        }

        public void MarkSpot(string name, Vector3 at)
        {
            if (string.IsNullOrEmpty(name)) return;
            spots.Add(new KeyValuePair<string, Vector3>(name, at));
        }

        public string BoardLines(string language)
        {
            return board != null ? board.Lines(language) : "";
        }

        public void Note(ObjectiveKind kind, string key, int amount)
        {
            if (board == null || extracted) return;
            int before = board.Tally;
            var finished = board.Note(kind, key, amount);
            if (board.Tally == before) return;
            foreach (var spec in finished) GameplayFeedback.Toast(ObjectiveBoard.Finished(spec, null));
            OnObjectivesChanged?.Invoke();
        }

        public void Waive(ObjectiveKind kind, string key)
        {
            if (board != null && board.Waive(kind, key) > 0) OnObjectivesChanged?.Invoke();
        }

        public void NotePickup(ItemCategory category, string itemId, int count)
        {
            if (board == null) return;
            Note(ObjectiveKind.Collect, category.ToString(), count);
            if (category == ItemCategory.KeyItem) Note(ObjectiveKind.Retrieve, itemId, count);
        }

        public void NoteKillAt(Vector3 at)
        {
            if (board == null || !board.Wants(ObjectiveKind.ClearNest)) return;
            string near = Near(at, ObjectiveBoard.NestRadius, "nest");
            if (near != null) Note(ObjectiveKind.ClearNest, near, 1);
        }

        private string Near(Vector3 at, float radius, string only)
        {
            float best = radius * radius;
            string found = null;
            foreach (var spot in spots)
            {
                if (only != null && spot.Key != only) continue;
                Vector3 gap = spot.Value - at;
                gap.y = 0f;
                if (gap.sqrMagnitude > best) continue;
                best = gap.sqrMagnitude;
                found = spot.Key;
            }
            return found;
        }

        private void Update()
        {
            if (board == null || !board.Wants(ObjectiveKind.Reach)) return;
            reachTimer -= Time.deltaTime;
            if (reachTimer > 0f) return;
            reachTimer = 0.5f;
            var player = PlayerRegistry.Current;
            if (player == null) return;
            string near = Near(player.transform.position, ObjectiveBoard.ReachRadius, null);
            if (near != null) Note(ObjectiveKind.Reach, near, 1);
        }

        public string PackBoard() => board != null ? board.PackProgress() : "";

        public void RestoreBoard(string packed)
        {
            if (board == null || string.IsNullOrEmpty(packed)) return;
            board.RestoreProgress(packed);
            OnObjectivesChanged?.Invoke();
        }

        public void ResetProgress()
        {
            kills = 0;
            scrap = 0;
            extracted = false;
            poiFound = false;
            poiRole = "";
            board = null;
            spots.Clear();
            OnObjectivesChanged?.Invoke();
        }
    }
}

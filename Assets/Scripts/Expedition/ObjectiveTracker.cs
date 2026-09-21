using System;
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

        public int KillGoal => killGoal;
        public int ScrapGoal => scrapGoal;
        public int Kills => kills;
        public int Scrap => scrap;
        public bool Extracted => extracted;
        public bool ReadyToExtract => kills >= killGoal && scrap >= scrapGoal;
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

        public void ResetProgress()
        {
            kills = 0;
            scrap = 0;
            extracted = false;
            OnObjectivesChanged?.Invoke();
        }
    }
}

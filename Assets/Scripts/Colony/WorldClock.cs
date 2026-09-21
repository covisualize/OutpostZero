using System;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    public class WorldClock : MonoBehaviour
    {
        public static WorldClock Instance { get; private set; }

        [SerializeField] private int day = 1;
        [SerializeField] private float hour = 18.5f;

        public int Day => day;
        public float Hour => hour;
        public event Action OnClockChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            if (state != GameState.ExpeditionActive && state != GameState.RaidActive) return;
            Advance(Time.deltaTime / 40f);
        }

        public void Advance(float hours)
        {
            hour += hours;
            while (hour >= 24f)
            {
                hour -= 24f;
                day++;
            }
            OnClockChanged?.Invoke();
        }

        public void SleepUntilMorning()
        {
            day++;
            hour = 6.5f;
            OnClockChanged?.Invoke();
            GameplayFeedback.Toast("Day " + day + "  morning watch");
        }

        public void Set(int nextDay, float nextHour)
        {
            day = Mathf.Max(1, nextDay);
            hour = Mathf.Repeat(nextHour, 24f);
            OnClockChanged?.Invoke();
        }

        public string Label => "Day " + day + "  " + Mathf.FloorToInt(hour).ToString("00") + ":" + Mathf.FloorToInt((hour % 1f) * 60f).ToString("00");
    }
}

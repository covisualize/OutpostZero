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
        public DayPhase Phase => ClockPhase.Of(hour);
        public event Action OnClockChanged;

        /// <summary>Raised when the clock runs from one phase into the next. Loading or jumping with <see cref="Set"/> does not raise it.</summary>
        public static event Action<DayPhase, DayPhase> PhaseTurned;

        /// <summary>Raised with the day that ended and the day that began.</summary>
        public static event Action<int, int> DayTurned;

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
            int from = day;
            var phase = Phase;
            hour += hours;
            while (hour >= 24f)
            {
                hour -= 24f;
                day++;
            }
            if (day != from && ColonyStorage.Instance != null)
                ColonyStorage.Instance.SetShots(RaidCall.Carry(ColonyStorage.Instance.Shots, from, day));
            if (day != from) DayTurned?.Invoke(from, day);
            Turn(phase);
            OnClockChanged?.Invoke();
        }

        private void Turn(DayPhase from)
        {
            var now = Phase;
            if (now != from) PhaseTurned?.Invoke(from, now);
        }

        public void SleepUntilMorning()
        {
            int from = day;
            var phase = Phase;
            day++;
            hour = 6.5f;
            if (ColonyStorage.Instance != null)
                ColonyStorage.Instance.SetShots(RaidCall.Carry(ColonyStorage.Instance.Shots, from, day));
            DayTurned?.Invoke(from, day);
            if (phase == DayPhase.Morning) PhaseTurned?.Invoke(phase, DayPhase.Morning);
            else Turn(phase);
            OnClockChanged?.Invoke();
            GameplayFeedback.Toast(ClockFace.Morning(day, null));
        }

        public void Set(int nextDay, float nextHour)
        {
            day = Mathf.Max(1, nextDay);
            hour = Mathf.Repeat(nextHour, 24f);
            OnClockChanged?.Invoke();
        }

        public string Label => ClockFace.Read(day, hour, null) + "  " + ClockPhase.Name(Phase, null);
    }
}

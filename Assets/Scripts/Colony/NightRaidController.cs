using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    public class NightRaidController : MonoBehaviour
    {
        public static NightRaidController Instance { get; private set; }

        [SerializeField] private float duration = 75f;
        private float endsAt;
        private float nextStrike;
        private bool running;

        public bool Running => running;
        public float Remaining => running ? Mathf.Max(0f, endsAt - Time.time) : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Begin()
        {
            if (GameManager.Instance == null) return;
            running = true;
            endsAt = Time.time + duration;
            nextStrike = Time.time + 1.2f;
            GameManager.Instance.SetState(GameState.RaidActive);
            if (HordeDirector.Instance != null) HordeDirector.Instance.BeginRaid();
            GameplayFeedback.Toast("Night raid — hold the gate");
        }

        private void Update()
        {
            if (!running) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.RaidActive)
            {
                running = false;
                return;
            }
            if (Time.time >= nextStrike)
            {
                nextStrike = Time.time + 1.2f;
                int guards = 0;
                if (SurvivorRoster.Instance != null)
                {
                    foreach (var survivor in SurvivorRoster.Instance.Survivors)
                    {
                        if (survivor.alive && survivor.task == "Guard") guards++;
                    }
                }
                int hit = Mathf.Max(2, 9 - guards * 3);
                if (GridBuilder.Instance != null && GridBuilder.Instance.BarricadeCount() > 0)
                {
                    GridBuilder.Instance.StrikeBarricade(hit);
                }
            }
            if (Time.time < endsAt) return;
            running = false;
            int security = ColonyStorage.Instance != null ? ColonyStorage.Instance.Security : 0;
            bool held = security > 0 || (HordeDirector.Instance != null && HordeDirector.Instance.Tension < 80f);
            if (held)
            {
                SurvivorRoster.Instance?.TickTasks();
                if (SurvivorRoster.Instance != null)
                {
                    foreach (var survivor in SurvivorRoster.Instance.Survivors)
                    {
                        if (survivor.alive) survivor.morale = Mathf.Min(100f, survivor.morale + 6f);
                    }
                }
                GameplayFeedback.Toast("The gate held");
            }
            else
            {
                ColonyStorage.Instance?.AddScrap(-6);
                GameplayFeedback.Toast("The raid broke the stores");
            }
            GameManager.Instance?.SetState(GameState.CampManagement);
        }
    }
}

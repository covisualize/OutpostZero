using System;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Player
{
    public class SurvivalNeeds : MonoBehaviour
    {
        [SerializeField] private float hunger = 80f;
        [SerializeField] private float thirst = 75f;
        [SerializeField] private float fatigue = 20f;

        public float Hunger => hunger;
        public float Thirst => thirst;
        public float Fatigue => fatigue;
        public event Action OnNeedsChanged;

        public float StaminaRegenMultiplier => NeedsPressure.Regen(hunger);
        public float StaminaCap(float max) => NeedsPressure.StaminaCap(thirst, max);
        public float AimScale => NeedsPressure.Aim(fatigue);

        private void Update()
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            if (state != GameState.ExpeditionActive && state != GameState.RaidActive) return;

            hunger = Mathf.Max(0f, hunger - NeedsPressure.HungerPerSecond * Time.deltaTime);
            thirst = Mathf.Max(0f, thirst - NeedsPressure.ThirstPerSecond * Time.deltaTime);
            fatigue = Mathf.Min(100f, fatigue + NeedsPressure.FatiguePerSecond * Time.deltaTime);
        }

        public void Eat(float amount)
        {
            hunger = Mathf.Min(100f, hunger + amount);
            OnNeedsChanged?.Invoke();
            Mirror();
        }

        public void Drink(float amount)
        {
            thirst = Mathf.Min(100f, thirst + amount);
            OnNeedsChanged?.Invoke();
            Mirror();
        }

        public void Rest(float amount)
        {
            fatigue = Mathf.Max(0f, fatigue - amount);
            hunger = Mathf.Max(0f, hunger - 8f);
            thirst = Mathf.Max(0f, thirst - 8f);
            OnNeedsChanged?.Invoke();
            Mirror();
        }

        public void SetFatigue(float value)
        {
            fatigue = UnityEngine.Mathf.Clamp(value, 0f, 100f);
        }

        public void Apply(float hungerValue, float thirstValue, float fatigueValue)
        {
            hunger = Mathf.Clamp(hungerValue, 0f, 100f);
            thirst = Mathf.Clamp(thirstValue, 0f, 100f);
            fatigue = Mathf.Clamp(fatigueValue, 0f, 100f);
            OnNeedsChanged?.Invoke();
            Mirror();
        }

        private void Mirror()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement) return;
            OutpostZero.Colony.SurvivorRoster.Instance?.CopyLeaderNeeds(hunger, thirst);
        }
    }
}

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

        public float StaminaRegenMultiplier
        {
            get
            {
                float penalty = 1f;
                if (hunger < 25f) penalty *= 0.65f;
                if (thirst < 25f) penalty *= 0.7f;
                if (fatigue > 75f) penalty *= 0.6f;
                return penalty;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            var state = GameManager.Instance.CurrentState;
            if (state != GameState.ExpeditionActive && state != GameState.RaidActive) return;

            hunger = Mathf.Max(0f, hunger - 0.35f * Time.deltaTime);
            thirst = Mathf.Max(0f, thirst - 0.5f * Time.deltaTime);
            fatigue = Mathf.Min(100f, fatigue + 0.22f * Time.deltaTime);
        }

        public void Eat(float amount)
        {
            hunger = Mathf.Min(100f, hunger + amount);
            OnNeedsChanged?.Invoke();
        }

        public void Drink(float amount)
        {
            thirst = Mathf.Min(100f, thirst + amount);
            OnNeedsChanged?.Invoke();
        }

        public void Rest(float amount)
        {
            fatigue = Mathf.Max(0f, fatigue - amount);
            hunger = Mathf.Max(0f, hunger - 8f);
            thirst = Mathf.Max(0f, thirst - 8f);
            OnNeedsChanged?.Invoke();
        }

        public void Apply(float hungerValue, float thirstValue, float fatigueValue)
        {
            hunger = Mathf.Clamp(hungerValue, 0f, 100f);
            thirst = Mathf.Clamp(thirstValue, 0f, 100f);
            fatigue = Mathf.Clamp(fatigueValue, 0f, 100f);
            OnNeedsChanged?.Invoke();
        }
    }
}

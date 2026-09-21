using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Colony
{
    public enum StationKind
    {
        Workbench,
        Campfire,
        MedicalCot,
        Water,
        Merchant,
        Generator
    }

    public class CampStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private StationKind kind;

        public StationKind Kind => kind;
        public string Prompt
        {
            get
            {
                switch (kind)
                {
                    case StationKind.Workbench: return "Use workbench";
                    case StationKind.Campfire: return "Cook and rest";
                    case StationKind.MedicalCot: return "Treat wounds";
                    case StationKind.Water: return "Draw water";
                    case StationKind.Generator: return "Fuel the generator";
                    default: return "Trade";
                }
            }
        }

        public void Configure(StationKind stationKind) => kind = stationKind;

        public bool CanInteract(PlayerInventory inventory) => true;

        public void Interact(PlayerInventory inventory)
        {
            switch (kind)
            {
                case StationKind.Workbench:
                    GameplayFeedback.Toast("Workbench open — craft from the camp menu");
                    break;
                case StationKind.Campfire:
                    var needs = inventory != null ? inventory.GetComponent<SurvivalNeeds>() : null;
                    needs?.Eat(25f);
                    needs?.Rest(30f);
                    if (ColonyStorage.Instance != null && ColonyStorage.Instance.Food > 0)
                    {
                        ColonyStorage.Instance.AddFood(-1);
                    }
                    WorldClock.Instance?.SleepUntilMorning();
                    SurvivorRoster.Instance?.TickTasks();
                    SurvivorRoster.Instance?.EndDay(false);
                    break;
                case StationKind.MedicalCot:
                    var health = inventory != null ? inventory.GetComponent<Combat.HealthSystem>() : null;
                    health?.Heal(40f);
                    inventory?.GetComponent<StatusEffectController>()?.ClearInjury();
                    GameplayFeedback.Toast("Wounds treated");
                    break;
                case StationKind.Water:
                    inventory?.GetComponent<SurvivalNeeds>()?.Drink(45f);
                    ColonyStorage.Instance?.AddWater(1);
                    GameplayFeedback.Toast("Water collected");
                    break;
                case StationKind.Generator:
                    if (ColonyStorage.Instance != null && ColonyStorage.Instance.TrySpendScrap(4))
                    {
                        CampServices.Instance?.Refuel(8f);
                        GameplayFeedback.Toast("Generator fueled");
                    }
                    else GameplayFeedback.Toast("Need 4 camp scrap");
                    break;
                default:
                    FactionTrade.Instance?.Toggle();
                    break;
            }
        }
    }
}

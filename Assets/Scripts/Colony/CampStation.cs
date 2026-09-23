using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

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
        public string Prompt => StallVoice.Prompt(kind, null);

        public void Configure(StationKind stationKind) => kind = stationKind;

        public bool CanInteract(PlayerInventory inventory) => true;

        public void Interact(PlayerInventory inventory)
        {
            switch (kind)
            {
                case StationKind.Workbench:
                    GameplayFeedback.Toast(Loc.T("stall.open"));
                    break;
                case StationKind.Campfire:
                    var needs = inventory != null ? inventory.GetComponent<SurvivalNeeds>() : null;
                    needs?.Eat(25f);
                    if (ColonyStorage.Instance != null && ColonyStorage.Instance.Food > 0)
                    {
                        ColonyStorage.Instance.AddFood(-1);
                    }
                    if (NightRaidController.Instance != null && NightRaidController.Instance.HoldTheNight()) return;
                    bool cot = CampServices.Instance != null && CampServices.Instance.CotOnline;
                    needs?.Rest(NightRest.Amount(cot));
                    GameplayFeedback.Toast(Loc.T(cot ? "camp.cot_sleep" : "camp.slept"));
                    WorldClock.Instance?.SleepUntilMorning();
                    SurvivorRoster.Instance?.TickTasks();
                    SurvivorRoster.Instance?.EndDay(false);
                    break;
                case StationKind.MedicalCot:
                    var health = inventory != null ? inventory.GetComponent<Combat.HealthSystem>() : null;
                    health?.Heal(40f);
                    inventory?.GetComponent<StatusEffectController>()?.ClearInjury();
                    GameplayFeedback.Toast(Loc.T("stall.wounds"));
                    break;
                case StationKind.Water:
                    inventory?.GetComponent<SurvivalNeeds>()?.Drink(45f);
                    ColonyStorage.Instance?.AddWater(1);
                    GameplayFeedback.Toast(Loc.T("stall.drawn"));
                    break;
                case StationKind.Generator:
                    if (ColonyStorage.Instance != null && ColonyStorage.Instance.TrySpendScrap(4))
                    {
                        CampServices.Instance?.Refuel(8f);
                        GameplayFeedback.Toast(Loc.T("stall.fueled"));
                    }
                    else GameplayFeedback.Toast(Loc.T("stall.scrap"));
                    break;
                default:
                    FactionTrade.Instance?.Toggle();
                    break;
            }
        }
    }
}

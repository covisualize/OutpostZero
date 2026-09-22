using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The district's marked room. Searching it records the cache or the radio part.
    /// </summary>
    public class DistrictPoi : MonoBehaviour, IInteractable
    {
        private string role = "cache";
        private bool taken;

        public string Prompt => taken ? string.Empty : role == "radio" ? "Take the radio part" : "Search the cache";

        public void Configure(string poiRole)
        {
            role = string.IsNullOrEmpty(poiRole) ? "cache" : poiRole;
            taken = false;
        }

        public bool CanInteract(PlayerInventory inventory) => !taken;

        public void Interact(PlayerInventory inventory)
        {
            if (taken) return;
            taken = true;
            ObjectiveTracker.Instance?.MarkPoi();
            GameplayFeedback.Toast(role == "radio" ? "Radio part stowed" : "Cache searched");
        }
    }
}

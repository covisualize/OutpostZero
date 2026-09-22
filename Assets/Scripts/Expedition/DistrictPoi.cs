using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The district's marked room. Searching it records the cache or the radio part.
    /// </summary>
    public class DistrictPoi : MonoBehaviour, IInteractable
    {
        private string role = "cache";
        private string stamp = "";
        private bool taken;

        public string Prompt => taken ? string.Empty : role == "radio" ? StreetAsk.Radio(null) : StreetAsk.Cache(null);

        public void Configure(string poiRole)
        {
            role = string.IsNullOrEmpty(poiRole) ? "cache" : poiRole;
            taken = false;
        }

        public void Stamp(string mark)
        {
            stamp = mark ?? "";
        }

        public void Recall()
        {
            taken = true;
        }

        public bool CanInteract(PlayerInventory inventory) => !taken;

        public void Interact(PlayerInventory inventory)
        {
            if (taken) return;
            taken = true;
            ObjectiveTracker.Instance?.MarkPoi();
            if (!string.IsNullOrEmpty(stamp)) OutpostZero.Shell.WorldMapService.Instance?.NoteStreet(stamp);
            GameplayFeedback.Toast(StreetAsk.Stowed(role == "radio", null));
        }
    }
}

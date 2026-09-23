using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The district's marked room. Searching it records the cache or the radio part, and hands over the
    /// item a Retrieve objective sends the leader for. A pack too full to take it leaves the room unsearched.
    /// </summary>
    public class DistrictPoi : MonoBehaviour, IInteractable
    {
        private string role = "cache";
        private string stamp = "";
        private bool taken;
        private string grant = "";

        public string Grant => grant;

        public string Prompt => taken ? string.Empty : role == "radio" ? StreetAsk.Radio(null) : StreetAsk.Cache(null);

        public void Configure(string poiRole)
        {
            role = string.IsNullOrEmpty(poiRole) ? "cache" : poiRole;
            taken = false;
        }

        public void Hold(string itemId)
        {
            grant = ItemCatalog.Find(itemId) != null ? itemId : "";
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
            if (!string.IsNullOrEmpty(grant))
            {
                var record = ItemCatalog.Find(grant);
                if (inventory == null || !inventory.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight))
                {
                    GameplayFeedback.Toast(Loc.T("ask.part_full"));
                    return;
                }
            }
            taken = true;
            ObjectiveTracker.Instance?.MarkPoi();
            if (!string.IsNullOrEmpty(stamp)) OutpostZero.Shell.WorldMapService.Instance?.NoteStreet(stamp);
            GameplayFeedback.Toast(string.IsNullOrEmpty(grant) ? StreetAsk.Stowed(role == "radio", null) : Loc.T("ask.part"));
        }
    }
}

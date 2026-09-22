using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A stranded person. Once they agree to follow, reaching the extraction ring puts them on the roster.
    /// </summary>
    public class RescueFollower : MonoBehaviour, IInteractable
    {
        public static RescueFollower Current { get; private set; }

        private string personId = "";
        private string personName = "";
        private bool following;
        private bool joined;

        public string Name => personName;
        public bool Following => following;
        public string Prompt => joined || following ? string.Empty : StreetAsk.Along(personName, null);

        public static string Status()
        {
            var person = Current;
            if (person == null || person.joined) return "";
            if (!person.following) return StreetAsk.ToGate(person.personName, null);
            return StreetAsk.With(person.personName, null);
        }

        public void Configure(string id, string displayName)
        {
            personId = id ?? "";
            personName = string.IsNullOrEmpty(displayName) ? "Survivor" : displayName;
        }

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        public bool CanInteract(PlayerInventory inventory) => !joined && !following;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            following = true;
            GameplayFeedback.Toast(StreetAsk.With(personName, null));
        }

        private void Update()
        {
            if (joined || !following) return;
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var lead = player.transform.position;
            RescueBook.Step(transform.position.x, transform.position.z, lead.x, lead.z, 4.2f, Time.deltaTime, out float nextX, out float nextZ);
            transform.position = new Vector3(nextX, transform.position.y, nextZ);

            var gate = ExtractionZone.Current;
            if (gate == null) return;
            var spot = gate.transform.position;
            bool personThere = RescueBook.AtGate(nextX, nextZ, spot.x, spot.z, 3.4f);
            bool playerThere = RescueBook.AtGate(lead.x, lead.z, spot.x, spot.z, 3.4f);
            if (!personThere || !playerThere) return;
            TryJoin();
        }

        private void TryJoin()
        {
            if (joined) return;
            var roster = SurvivorRoster.Instance;
            if (roster == null || !roster.Adopt(personId, personName, Trait()))
            {
                GameplayFeedback.Toast(FightSay.Roster(null));
                joined = true;
                following = false;
                return;
            }
            joined = true;
            following = false;
            GameplayFeedback.Toast(StreetAsk.Stays(personName, null));
            gameObject.SetActive(false);
        }

        private string Trait()
        {
            var offer = RescueBook.For(personId == "rescue_hospital" ? "old_hospital" : personId == "rescue_police" ? "police_station" : personId == "rescue_mall" ? "mall" : "");
            return string.IsNullOrEmpty(offer.Trait) ? "Steady" : offer.Trait;
        }
    }
}

using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The caravan's porter on a visit day. Asked along, they follow the leader and finish the Caravan's escort
    /// quest once both stand in the extraction ring. Bites wear them down the way they wear a rescued survivor,
    /// and a fall drops the escort for the run.
    /// </summary>
    public class CaravanPorter : MonoBehaviour, IInteractable
    {
        public const float Pace = 4.4f;
        public const float GateRadius = 3.4f;

        private bool following;
        private bool done;
        private int bites;
        private float lastBite;

        public bool Following => following;
        public int Bites => bites;

        public string Prompt => following || done ? string.Empty : StallVoice.Porter(false, null);

        public bool CanInteract(PlayerInventory inventory) => !following && !done;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            following = true;
            GameplayFeedback.Toast(StallVoice.Porter(true, null));
        }

        private void Update()
        {
            if (done || !following) return;
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var lead = player.transform.position;
            RescueBook.Step(transform.position.x, transform.position.z, lead.x, lead.z, FollowLimp.Pace(Pace, bites), Time.deltaTime, out float nextX, out float nextZ);
            transform.position = new Vector3(nextX, transform.position.y, nextZ);

            if (FollowBite.Due(lastBite, Time.time, ZombieAI.Nearest(nextX, nextZ))) Bite(Time.time);
            if (done) return;

            var gate = ExtractionZone.Current;
            if (gate == null) return;
            var spot = gate.transform.position;
            if (!RescueBook.AtGate(nextX, nextZ, spot.x, spot.z, GateRadius) || !RescueBook.AtGate(lead.x, lead.z, spot.x, spot.z, GateRadius)) return;
            done = true;
            following = false;
            ObjectiveTracker.Instance?.Note(ObjectiveKind.Escort, FactionQuest.Porter, 1);
            gameObject.SetActive(false);
        }

        private void Bite(float now)
        {
            lastBite = now;
            if (FollowFall.Drops(bites))
            {
                done = true;
                following = false;
                GameplayFeedback.Toast(StallVoice.PorterFell(null));
                ObjectiveTracker.Instance?.Waive(ObjectiveKind.Escort, FactionQuest.Porter);
                gameObject.SetActive(false);
                return;
            }
            bites = FollowBite.After(bites);
        }
    }
}

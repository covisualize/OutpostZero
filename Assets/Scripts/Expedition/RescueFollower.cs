using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Sensory;
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
        private float lastCry;
        private float lastAid;

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
            if (StraggleCall.Due(lastCry, Time.time, ZombieAI.Nearest(nextX, nextZ)))
            {
                lastCry = Time.time;
                GameplayFeedback.Toast(StreetAsk.Cry(personName, null));
                AudioManager.Instance?.PlayAt("scream", transform.position, 0.45f);
                if (NoiseManager.Instance != null)
                    NoiseManager.Instance.EmitNoise(transform.position, StraggleCall.Radius, 0.8f, NoiseType.ZombieScream, gameObject);
            }

            if (StreetAid.Due(lastAid, Time.time, ZombieAI.Nearest(nextX, nextZ)))
            {
                var foe = ZombieAI.Closest(nextX, nextZ, StreetAid.Reach);
                if (foe != null)
                {
                    lastAid = Time.time;
                    var health = foe.GetComponent<HealthSystem>();
                    Vector3 aim = foe.transform.position - transform.position;
                    aim.y = 0f;
                    if (aim.sqrMagnitude < 0.0001f) aim = transform.forward;
                    if (health != null && !health.IsDead)
                        health.TakeDamage(StreetAid.Damage, foe.transform.position + Vector3.up, aim.normalized, gameObject);
                    GameplayFeedback.Toast(StreetAid.Line(personName, null));
                    if (NoiseManager.Instance != null)
                        NoiseManager.Instance.EmitNoise(transform.position, StreetAid.Noise, 0.7f, NoiseType.MeleeSwing, gameObject);
                }
            }

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

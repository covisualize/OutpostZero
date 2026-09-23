using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// The companion in the street: a stand-in body that trails the leader, fires at the nearest zombie in range on
    /// a Guard-set cadence, and takes bites the way a rescued survivor does. A fourth bite drops them; the leader's
    /// return or death sends them home with whatever they carry.
    /// </summary>
    public class CompanionFollower : MonoBehaviour
    {
        public static CompanionFollower Current { get; private set; }

        private string survivorId = "";
        private int combat;
        private int bites;
        private bool fell;
        private float lastBite;
        private float lastShot;

        public string SurvivorId => survivorId;
        public int Bites => bites;
        public bool Fell => fell;

        public static void Raise(Survivor mate)
        {
            Dismiss();
            var player = PlayerRegistry.Current;
            if (mate == null || player == null) return;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Companion_" + mate.id;
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            body.transform.position = player.transform.position - player.transform.forward * 1.6f + Vector3.up * 0.95f;
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.36f, 0.44f, 0.52f);
            var follower = body.AddComponent<CompanionFollower>();
            follower.survivorId = mate.id;
            follower.combat = mate.combat;
            Current = follower;
            GameplayFeedback.Toast(mate.displayName + " " + OutpostZero.Shell.Loc.T("companion.along"));
        }

        /// <summary>Hands the trip's bites to the roster and removes the body.</summary>
        public static void Home(bool cameHome)
        {
            var follower = Current;
            if (follower == null) return;
            SurvivorRoster.Instance?.CompanionHome(follower.survivorId, follower.bites, follower.fell, cameHome);
            Dismiss();
        }

        public static void Dismiss()
        {
            if (Current == null) return;
            Destroy(Current.gameObject);
            Current = null;
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (fell) return;
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var lead = player.transform.position;
            RescueBook.Step(transform.position.x, transform.position.z, lead.x, lead.z, FollowLimp.Pace(CompanionKit.Pace, bites), Time.deltaTime, out float nextX, out float nextZ);
            transform.position = new Vector3(nextX, transform.position.y, nextZ);

            float nearest = ZombieAI.Nearest(nextX, nextZ);
            if (FollowBite.Due(lastBite, Time.time, nearest)) Bite(Time.time);
            if (fell) return;
            if (CompanionKit.Fires(lastShot, Time.time, combat, nearest)) Shoot(Time.time);
        }

        private void Shoot(float now)
        {
            var target = ZombieAI.NearestAlive(transform.position);
            var health = target != null ? target.GetComponent<HealthSystem>() : null;
            if (health == null) return;
            lastShot = now;
            var from = transform.position + Vector3.up * 0.4f;
            var to = target.transform.position + Vector3.up;
            health.TakeDamage(CompanionKit.Damage(combat), to, (to - from).normalized, gameObject);
        }

        private void Bite(float now)
        {
            lastBite = now;
            if (FollowFall.Drops(bites))
            {
                fell = true;
                gameObject.SetActive(false);
                return;
            }
            bites = FollowBite.After(bites);
        }
    }
}

using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    public class ExtractionZone : MonoBehaviour
    {
        [SerializeField] private float radius = 3.2f;
        private bool inside;
        private bool finished;
        private float held;

        public static ExtractionZone Current { get; private set; }
        public float Hold => held;
        public bool Holding => inside && held > 0.05f;

        private void Awake()
        {
            Current = this;
        }

        public static void MoveTo(Vector3 position)
        {
            var zone = FindFirstObjectByType<ExtractionZone>();
            if (zone == null) zone = Create(position);
            zone.transform.position = position;
        }

        public static ExtractionZone Create(Vector3 position)
        {
            var existing = FindFirstObjectByType<ExtractionZone>();
            if (existing != null) return existing;

            var zone = new GameObject("ExtractionZone");
            zone.transform.position = position;
            var marker = zone.AddComponent<ExtractionZone>();
            var trigger = zone.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = marker.radius;
            var body = zone.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            return marker;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            inside = true;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.ExpeditionActive) return;
            var tracker = ObjectiveTracker.Instance;
            if (tracker != null && !tracker.ReadyToExtract) GameplayFeedback.Toast(GateLine.Quota(null));
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            inside = false;
            held = 0f;
        }

        private void Update()
        {
            if (finished) return;
            bool live = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ExpeditionActive;
            var tracker = ObjectiveTracker.Instance;
            bool ready = tracker != null && tracker.ReadyToExtract;
            bool threatened = inside && live && ZombiesNear();
            float next = ExtractWatch.Advance(held, Time.deltaTime, inside && live && ready, threatened);
            if (threatened && held > 0.2f) GameplayFeedback.Toast(GateLine.Close(null));
            held = next;
            if (!ExtractWatch.Ready(held) || GameManager.Instance == null) return;
            finished = true;
            held = 0f;
            tracker?.MarkExtracted();
            OutpostZero.Shell.CodexDirector.Hear("extract");
            GameManager.Instance.CompleteExpedition();
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null;
        }

        private bool ZombiesNear()
        {
            var hits = Physics.OverlapSphere(transform.position, ExtractWatch.ThreatRadius, GameLayers.EnemyMask);
            for (int i = 0; i < hits.Length; i++)
            {
                var zombie = hits[i].GetComponentInParent<ZombieAI>();
                if (zombie == null || zombie.CurrentState != ZombieAI.ZombieState.Dead) return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.6f, 0.35f);
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}

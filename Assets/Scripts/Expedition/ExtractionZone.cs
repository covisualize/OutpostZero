using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Expedition
{
    public class ExtractionZone : MonoBehaviour
    {
        [SerializeField] private float radius = 3.2f;

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
            if (!other.CompareTag("Player") && other.GetComponentInParent<Player.PlayerController>() == null) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.ExpeditionActive) return;
            var tracker = ObjectiveTracker.Instance;
            if (tracker != null && !tracker.ReadyToExtract)
            {
                GameplayFeedback.Toast("Objectives unfinished");
                return;
            }
            tracker?.MarkExtracted();
            OutpostZero.Shell.CodexDirector.Hear("extract");
            GameManager.Instance.CompleteExpedition();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.6f, 0.35f);
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}

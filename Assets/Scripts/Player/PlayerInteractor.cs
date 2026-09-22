using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;

namespace OutpostZero.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 2.2f;
        private PlayerInventory inventory;
        private PlayerController controller;
        private IInteractable current;

        public string Prompt => current != null ? current.Prompt : string.Empty;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!CanAct())
            {
                current = null;
                return;
            }

            current = FindInteractable();
            if (ExpeditionInput.InteractPressed && current != null)
            {
                current.Interact(inventory);
            }

            if (ExpeditionInput.TakedownPressed)
            {
                TryTakedown();
            }

            if (ExpeditionInput.ThrowPressed)
            {
                ThrowHeldItem();
            }
        }

        private bool CanAct()
        {
            if (GameManager.Instance == null) return true;
            var state = GameManager.Instance.CurrentState;
            return state == GameState.ExpeditionActive || state == GameState.RaidActive || state == GameState.CampManagement;
        }

        private IInteractable FindInteractable()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, reach);
            IInteractable best = null;
            float bestDist = reach;
            foreach (var hit in hits)
            {
                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract(inventory)) continue;
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = interactable;
                }
            }
            return best;
        }

        private void TryTakedown()
        {
            if (controller != null && !controller.IsCrouching) return;
            Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f, GameLayers.EnemyMask);
            foreach (var hit in hits)
            {
                var zombie = hit.GetComponentInParent<ZombieAI>();
                if (zombie == null || zombie.CurrentState == ZombieAI.ZombieState.Dead) continue;
                if (zombie.CurrentState == ZombieAI.ZombieState.Chase || zombie.CurrentState == ZombieAI.ZombieState.Attack) continue;
                Vector3 toZombie = zombie.transform.position - transform.position;
                toZombie.y = 0f;
                if (Vector3.Dot(zombie.transform.forward, toZombie.normalized) < 0.35f) continue;
                var health = zombie.GetComponent<HealthSystem>();
                if (health == null) continue;
                health.TakeDamage(999f, zombie.transform.position, transform.forward, gameObject);
                CombatEvents.RaiseKill(zombie.gameObject, gameObject);
                GameplayFeedback.Toast("Silent takedown");
                return;
            }
        }

        private void ThrowHeldItem()
        {
            if (inventory != null && inventory.TryConsume("molotov"))
            {
                Launch(true);
                return;
            }
            if (inventory != null && inventory.TryConsume("noise_lure"))
            {
                Launch(false);
                return;
            }
            GameplayFeedback.Toast("No throwable");
        }

        private void Launch(bool molotov)
        {
            var lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = molotov ? "Molotov" : "NoiseLure";
            lure.transform.position = transform.position + Vector3.up * 1.4f + transform.forward;
            lure.transform.localScale = Vector3.one * 0.25f;
            var body = lure.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(transform.forward * ThrowArc.Forward + Vector3.up * ThrowArc.Lift, ForceMode.VelocityChange);
            var thrown = lure.AddComponent<ThrownHazard>();
            thrown.Configure(molotov);
        }
    }

    public class ThrownHazard : MonoBehaviour
    {
        [SerializeField] private bool molotov;
        private bool popped;

        public void Configure(bool fire) => molotov = fire;

        private void Start()
        {
            Invoke(nameof(Pop), molotov ? 1.1f : 0.7f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!popped && collision.relativeVelocity.magnitude > 2f) Pop();
        }

        private void Pop()
        {
            if (popped) return;
            popped = true;
            Vector3 origin = transform.position;
            if (Sensory.NoiseManager.Instance != null)
            {
                Sensory.NoiseManager.Instance.EmitNoise(origin, ThrowArc.NoiseRadius(molotov), 1f, molotov ? NoiseType.Explosion : NoiseType.ObjectBroken, gameObject);
            }
            if (molotov)
            {
                Collider[] hits = Physics.OverlapSphere(origin, 3.2f);
                foreach (var hit in hits)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(28f, hit.bounds.center, (hit.transform.position - origin).normalized, gameObject);
                    }
                }
                CombatEvents.RaiseHit(origin, Vector3.up, gameObject);
            }
            GameplayFeedback.Toast(molotov ? "Molotov burst" : "Lure clattered");
            Destroy(gameObject);
        }
    }
}

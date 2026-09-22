using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 2.2f;
        private PlayerInventory inventory;
        private PlayerController controller;
        private IInteractable current;
        private ZombieAI marked;
        private float windup = -1f;

        public string Prompt => current != null ? current.Prompt : string.Empty;
        public bool TakingDown => windup >= 0f;

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

            if (TakingDown) AdvanceTakedown();
            else if (ExpeditionInput.TakedownPressed) TryTakedown();

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
            Collider[] hits = Physics.OverlapSphere(transform.position, QuietKill.Reach, GameLayers.EnemyMask);
            foreach (var hit in hits)
            {
                var zombie = hit.GetComponentInParent<ZombieAI>();
                if (!Eligible(zombie)) continue;
                marked = zombie;
                windup = 0f;
                GameplayFeedback.Toast("Takedown");
                return;
            }
        }

        private void AdvanceTakedown()
        {
            if (!Eligible(marked))
            {
                marked = null;
                windup = -1f;
                GameplayFeedback.Toast("Takedown slipped");
                return;
            }
            windup += Time.deltaTime;
            if (!QuietKill.Lands(windup)) return;
            var health = marked.GetComponent<HealthSystem>();
            if (health != null) health.TakeDamage(999f, marked.transform.position, transform.forward, gameObject);
            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(marked.transform.position, QuietKill.Noise, 0.45f, NoiseType.MeleeSwing, gameObject);
            CombatEvents.RaiseKill(marked.gameObject, gameObject);
            marked = null;
            windup = -1f;
            GameplayFeedback.Toast("Down");
        }

        private bool Eligible(ZombieAI zombie)
        {
            if (zombie == null) return false;
            bool alert = zombie.CurrentState == ZombieAI.ZombieState.Chase || zombie.CurrentState == ZombieAI.ZombieState.Attack;
            bool dead = zombie.CurrentState == ZombieAI.ZombieState.Dead;
            Vector3 toZombie = zombie.transform.position - transform.position;
            toZombie.y = 0f;
            float distance = toZombie.magnitude;
            float dot = distance > 0.001f ? Vector3.Dot(zombie.transform.forward, toZombie / distance) : 0f;
            bool crouched = controller == null || controller.IsCrouching;
            return QuietKill.Victim(crouched, dead, alert, distance, dot);
        }

        public bool ThrowId(string id)
        {
            int kind = TossKind.Of(id);
            if (kind == TossKind.None) return false;
            if (inventory == null || !inventory.TryConsume(id)) return false;
            if (kind == TossKind.Fire) Launch(true);
            else if (kind == TossKind.Flare) LaunchFlare();
            else if (kind == TossKind.Bomb) LaunchBomb();
            else Launch(false);
            return true;
        }

        private void ThrowHeldItem()
        {
            if (ThrowId("molotov")) return;
            if (ThrowId("noise_lure")) return;
            if (ThrowId("flare")) return;
            if (ThrowId("pipe_bomb")) return;
            GameplayFeedback.Toast(Loc.T("toss.none"));
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

        private void LaunchFlare()
        {
            var lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = "Flare";
            lure.transform.position = transform.position + Vector3.up * 1.4f + transform.forward;
            lure.transform.localScale = Vector3.one * 0.18f;
            var body = lure.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(transform.forward * ThrowArc.Forward + Vector3.up * ThrowArc.Lift, ForceMode.VelocityChange);
            lure.AddComponent<ThrownHazard>().ConfigureFlare();
        }

        private void LaunchBomb()
        {
            var lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = "PipeBomb";
            lure.transform.position = transform.position + Vector3.up * 1.4f + transform.forward;
            lure.transform.localScale = Vector3.one * 0.22f;
            var body = lure.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(transform.forward * ThrowArc.Forward + Vector3.up * ThrowArc.Lift, ForceMode.VelocityChange);
            lure.AddComponent<ThrownHazard>().ConfigureBomb();
        }
    }

    public class ThrownHazard : MonoBehaviour
    {
        [SerializeField] private bool molotov;
        [SerializeField] private bool flare;
        [SerializeField] private bool bomb;
        private bool popped;
        private float age = -1f;

        public void Configure(bool fire) => molotov = fire;

        public void ConfigureFlare() => flare = true;

        public void ConfigureBomb() => bomb = true;

        private void Start()
        {
            Invoke(nameof(Pop), bomb ? PipeBlast.Fuse : flare ? 0.8f : molotov ? 1.1f : 0.7f);
        }

        private void Update()
        {
            if (!flare || !popped) return;
            float next = age + Time.deltaTime;
            if (FlareClock.PulseDue(age, next)) EmitCall();
            age = next;
            if (!FlareClock.Lit(age)) Destroy(gameObject);
        }

        private void EmitCall()
        {
            if (Sensory.NoiseManager.Instance == null) return;
            Sensory.NoiseManager.Instance.EmitNoise(transform.position, FlareClock.Radius, 0.8f, NoiseType.ObjectBroken, gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!popped && collision.relativeVelocity.magnitude > 2f) Pop();
        }

        private void Pop()
        {
            if (popped) return;
            popped = true;
            if (bomb)
            {
                Vector3 blast = transform.position;
                if (Sensory.NoiseManager.Instance != null)
                {
                    Sensory.NoiseManager.Instance.EmitNoise(blast, PipeBlast.Noise, 1f, NoiseType.Explosion, gameObject);
                }
                Collider[] caught = Physics.OverlapSphere(blast, PipeBlast.Radius);
                foreach (var hit in caught)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(PipeBlast.Damage, hit.bounds.center, (hit.transform.position - blast).normalized, gameObject);
                    }
                    var zombie = hit.GetComponentInParent<ZombieAI>();
                    if (zombie != null)
                    {
                        zombie.ApplyImpulse(hit.transform.position - blast, PipeBlast.Shove, PipeBlast.Stun);
                    }
                }
                CombatVfx.Burst(blast, HazardKind.Explosive);
                CombatEvents.RaiseHit(blast, Vector3.up, gameObject);
                GameplayFeedback.Toast("Pipe bomb burst");
                Destroy(gameObject);
                return;
            }
            if (flare)
            {
                age = 0f;
                EmitCall();
                var glow = gameObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.range = 9f;
                glow.intensity = 2.2f;
                glow.color = new Color(1f, 0.55f, 0.2f);
                var body = GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                var solid = GetComponent<Collider>();
                if (solid != null) solid.enabled = false;
                GameplayFeedback.Toast("Flare lit");
                return;
            }
            Vector3 origin = transform.position;
            if (Sensory.NoiseManager.Instance != null)
            {
                Sensory.NoiseManager.Instance.EmitNoise(origin, ThrowArc.NoiseRadius(molotov), 1f, molotov ? NoiseType.Explosion : NoiseType.ObjectBroken, gameObject);
            }
            if (molotov)
            {
                OutpostZero.Colony.GridBuilder.Instance?.IgniteNear(origin.x, origin.z, Time.time);
                Collider[] hits = Physics.OverlapSphere(origin, FirePatch.BurstRadius);
                foreach (var hit in hits)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(FirePatch.Burst, hit.bounds.center, (hit.transform.position - origin).normalized, gameObject);
                    }
                }
                var patch = new GameObject("FirePatch");
                patch.transform.position = origin;
                patch.AddComponent<GroundFire>();
                CombatEvents.RaiseHit(origin, Vector3.up, gameObject);
            }
            GameplayFeedback.Toast(molotov ? "Molotov burst" : "Lure clattered");
            Destroy(gameObject);
        }
    }
}

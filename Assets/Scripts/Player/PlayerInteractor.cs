using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float reach = 2.2f;
        private PlayerInventory inventory;
        private PlayerController controller;
        private HoverShell shell;
        private ThrowPreview arc;
        private IInteractable current;
        private ZombieAI marked;
        private float windup = -1f;
        private float sweep = -1f;

        public string Prompt => current != null ? current.Prompt : string.Empty;
        public IInteractable Current => current;
        public bool TakingDown => windup >= 0f;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            controller = GetComponent<PlayerController>();
            shell = GetComponent<HoverShell>();
            if (shell == null) shell = gameObject.AddComponent<HoverShell>();
            arc = Attach.Ensure<ThrowPreview>(gameObject);
            ThrowableBook.Ensure();
        }

        private void Update()
        {
            if (!CanAct())
            {
                current = null;
                shell?.Hold(null);
                arc?.Hide();
                return;
            }

            current = FindInteractable();
            shell?.Hold(current as Component);
            if (ExpeditionInput.InteractPressed && current != null)
            {
                current.Interact(inventory);
                sweep = 0f;
            }
            else if (sweep >= 0f && ExpeditionInput.InteractHeld)
            {
                sweep += Time.deltaTime;
                if (PackOps.Swept(sweep))
                {
                    sweep = -1f;
                    TakeEverything();
                }
            }
            else sweep = -1f;

            if (TakingDown) AdvanceTakedown();
            else if (ExpeditionInput.TakedownPressed) TryTakedown();

            if (ExpeditionInput.ThrowReleased)
            {
                arc?.Hide();
                ThrowHeldItem();
            }
            else if (ExpeditionInput.ThrowHeld && NextThrowable() != null) arc?.Show(transform.position, transform.forward);
            else arc?.Hide();
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

        private void TakeEverything()
        {
            var open = LootContainer.Open;
            if (open != null && Vector3.Distance(transform.position, open.transform.position) <= reach + 1f) open.TakeAll(inventory);
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, reach);
            foreach (var hit in hits)
            {
                var item = hit.GetComponentInParent<WorldItem>();
                if (item != null && item.CanInteract(inventory)) item.Interact(inventory);
            }
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
                GameplayFeedback.Toast(FightSay.Start(null));
                return;
            }
        }

        private void AdvanceTakedown()
        {
            if (!Eligible(marked))
            {
                marked = null;
                windup = -1f;
                GameplayFeedback.Toast(FightSay.Slip(null));
                return;
            }
            windup += Time.deltaTime;
            if (!QuietKill.Lands(windup)) return;
            var health = marked.GetComponent<HealthSystem>();
            if (health != null) health.TakeDamage(999f, marked.transform.position, transform.forward, gameObject);
            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(marked.transform.position, Sensory.NoiseTable.Radius(Sensory.NoiseTable.Takedown), Sensory.NoiseTable.Loud(Sensory.NoiseTable.Takedown), NoiseType.MeleeSwing, gameObject);
            CombatEvents.RaiseKill(marked.gameObject, gameObject);
            marked = null;
            windup = -1f;
            GameplayFeedback.Toast(FightSay.Down(null));
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
            var row = ThrowableTable.Of(id);
            if (row == null || row.Kind == TossKind.None) return false;
            if (inventory == null || !inventory.TryConsume(id)) return false;
            Launch(row);
            return true;
        }

        /// <summary>The first throwable in the pack, in the book's order: what a release of the throw key sends.</summary>
        public string NextThrowable()
        {
            if (inventory == null) return null;
            var rows = ThrowableTable.All;
            for (int i = 0; i < rows.Count; i++)
                if (inventory.Has(rows[i].Id)) return rows[i].Id;
            return null;
        }

        private void ThrowHeldItem()
        {
            string id = NextThrowable();
            if (id != null && ThrowId(id)) return;
            GameplayFeedback.Toast(Loc.T("toss.none"));
        }

        private void Launch(ThrowableTable.Row row)
        {
            var lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = row.Id;
            lure.transform.position = transform.position + Vector3.up * ThrowArc.Height + transform.forward;
            lure.transform.localScale = Vector3.one * row.Size;
            if (row.Kind == TossKind.Fire) MaterialLibrary.Dress(lure.GetComponent<Renderer>(), SurfaceFamily.Glass);
            else if (row.Kind != TossKind.Flare) MaterialLibrary.Dress(lure.GetComponent<Renderer>(), SurfaceFamily.MetalRusted);
            var body = lure.AddComponent<Rigidbody>();
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(transform.forward * ThrowArc.Forward + Vector3.up * ThrowArc.Lift, ForceMode.VelocityChange);
            lure.AddComponent<ThrownHazard>().Configure(row);
        }
    }

    public class ThrownHazard : MonoBehaviour
    {
        [SerializeField] private bool molotov;
        [SerializeField] private bool flare;
        [SerializeField] private bool bomb;
        private ThrowableTable.Row row = ThrowableTable.Of("street_bottle");
        private bool popped;
        private float age = -1f;

        public void Configure(ThrowableTable.Row thrown)
        {
            if (thrown == null) return;
            row = thrown;
            molotov = thrown.Kind == TossKind.Fire;
            flare = thrown.Kind == TossKind.Flare;
            bomb = thrown.Kind == TossKind.Bomb;
        }

        private void Start()
        {
            Invoke(nameof(Pop), row.Fuse);
        }

        private void Update()
        {
            if (!flare || !popped) return;
            float next = age + Time.deltaTime;
            if (FlareClock.PulseDue(age, next)) EmitCall();
            age = next;
            if (!FlareClock.Lit(age, row.Seconds)) Destroy(gameObject);
        }

        private void EmitCall()
        {
            if (Sensory.NoiseManager.Instance == null) return;
            Sensory.NoiseManager.Instance.EmitNoise(transform.position, row.Noise, 0.8f, NoiseType.ObjectBroken, gameObject);
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
                    Sensory.NoiseManager.Instance.EmitNoise(blast, row.Noise, 1f, NoiseType.Explosion, gameObject);
                }
                Collider[] caught = Physics.OverlapSphere(blast, row.Radius);
                CombatEvents.RaiseBlast(blast, row.Radius);
                BlastKill.Begin(blast, PipeBlast.Throw, row.Radius);
                try
                {
                    foreach (var hit in caught)
                    {
                        var damageable = hit.GetComponentInParent<IDamageable>();
                        if (damageable != null && !damageable.IsDead)
                        {
                            damageable.TakeDamage(row.Damage, hit.bounds.center, (hit.transform.position - blast).normalized, gameObject);
                        }
                        var zombie = hit.GetComponentInParent<ZombieAI>();
                        if (zombie != null)
                        {
                            zombie.ApplyImpulse(hit.transform.position - blast, PipeBlast.Shove, PipeBlast.Stun);
                        }
                    }
                }
                finally
                {
                    BlastKill.End();
                }
                CombatVfx.Burst(blast, HazardKind.Explosive);
                OilPatch.Blast(blast);
                CombatEvents.RaiseHit(blast, Vector3.up, gameObject);
                GameplayFeedback.Toast(FightSay.Burst(null));
                Destroy(gameObject);
                return;
            }
            if (flare)
            {
                age = 0f;
                EmitCall();
                var glow = gameObject.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.range = row.Radius;
                glow.intensity = 2.2f;
                glow.color = new Color(1f, 0.55f, 0.2f);
                var body = GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = true;
                var solid = GetComponent<Collider>();
                if (solid != null) solid.enabled = false;
                GameplayFeedback.Toast(FightSay.Flare(null));
                return;
            }
            Vector3 origin = transform.position;
            if (Sensory.NoiseManager.Instance != null)
            {
                Sensory.NoiseManager.Instance.EmitNoise(origin, row.Noise, 1f, molotov ? NoiseType.Explosion : NoiseType.ObjectBroken, gameObject);
            }
            if (molotov)
            {
                OutpostZero.Colony.GridBuilder.Instance?.IgniteNear(origin.x, origin.z, Time.time);
                OilPatch.Blast(origin);
                Collider[] hits = Physics.OverlapSphere(origin, row.Radius);
                foreach (var hit in hits)
                {
                    var damageable = hit.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(row.Damage, hit.bounds.center, (hit.transform.position - origin).normalized, gameObject);
                    }
                }
                var patch = new GameObject("FirePatch");
                patch.transform.position = origin;
                patch.AddComponent<GroundFire>();
                CombatEvents.RaiseHit(origin, Vector3.up, gameObject);
            }
            GameplayFeedback.Toast(FightSay.Impact(molotov, null));
            Destroy(gameObject);
        }
    }
}

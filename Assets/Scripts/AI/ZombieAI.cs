using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Combat;
using OutpostZero.Graphics;
using OutpostZero.Player;

namespace OutpostZero.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(HealthSystem))]
    public class ZombieAI : MonoBehaviour, INoiseListener
    {
        public enum ZombieState
        {
            Idle,
            Wander,
            InvestigateNoise,
            Chase,
            Attack,
            Stunned,
            Dead,
            Searching
        }

        [Header("State")]
        [SerializeField] private ZombieState currentState = ZombieState.Wander;
        public ZombieState CurrentState => currentState;

        [Header("Sensory Perception")]
        [SerializeField] private float sightRange = 14f;
        [SerializeField] private float sightAngle = 110f;
        [SerializeField] private LayerMask visionMask;
        [SerializeField] private float hearingSensitivity = 1.0f;

        [Header("Movement Speeds")]
        [SerializeField] private float wanderSpeed = 1.8f;
        [SerializeField] private float chaseSpeed = 4.6f;
        [SerializeField] private float wanderRadius = 12f;
        [SerializeField] private float minWaitAtWanderPoint = 2f;
        [SerializeField] private float maxWaitAtWanderPoint = 5f;

        [Header("Attack Settings")]
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackDamage = 18f;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private float hordeAlertRadius = 10f;

        [Header("Target & Memory")]
        [SerializeField] private Transform currentTarget;
        [SerializeField] private Vector3 lastKnownPosition;
        [SerializeField] private float investigationDuration = 6f;

        // Components
        private NavMeshAgent agent;
        private HealthSystem healthSystem;

        private float nextAttackTime = 0f;
        private float stateTimer = 0f;
        private float pendingStun = 0.8f;
        private SpecialBeat.Clock abilityClock;
        private Vector3 spawnOrigin;
        [SerializeField] private ZombieSpecialAbility specialAbility;
        private string archetypeId = "";
        private int sightToken;

        public Vector3 Position => transform.position;
        public float HearingSensitivity => hearingSensitivity;

        private void Reset()
        {
            visionMask = GameLayers.VisionOcclusionMask;
        }

        private void OnValidate()
        {
            visionMask = GameLayers.Resolve(visionMask, GameLayers.VisionOcclusionMask);
        }

        private void Awake()
        {
            visionMask = GameLayers.Resolve(visionMask, GameLayers.VisionOcclusionMask);
            agent = GetComponent<NavMeshAgent>();
            healthSystem = GetComponent<HealthSystem>();
            spawnOrigin = transform.position;

            healthSystem.OnDeath += HandleDeath;
            healthSystem.OnDamaged += HandleDamaged;
            GameLayers.ApplyRecursively(gameObject, GameLayers.Enemy);
            sightToken = QualityProfile.NextToken();
        }

        public void Configure(ZombieArchetype archetype)
        {
            if (archetype == null) return;

            wanderSpeed = archetype.wanderSpeed;
            chaseSpeed = archetype.chaseSpeed;
            attackDamage = archetype.attackDamage;
            attackCooldown = archetype.attackCooldown;
            attackRange = archetype.attackRange;
            sightRange = archetype.sightRange;
            sightAngle = archetype.sightAngle;
            hearingSensitivity = archetype.hearingSensitivity;
            hordeAlertRadius = archetype.hordeAlertRadius;
            specialAbility = archetype.specialAbility;
            archetypeId = archetype.id;
            visionMask = GameLayers.VisionOcclusionMask;

            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = chaseSpeed;
            }

            if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.Configure(archetype.maxHealth, archetype.armor);
            }
            CharacterVariety.Ensure(gameObject).Bind(archetypeId, false);
        }

        public void ResetForSpawn()
        {
            currentTarget = null;
            spawnOrigin = transform.position;
            if (healthSystem != null)
            {
                healthSystem.ResetHealth();
            }
            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
            }
            currentState = ZombieState.Idle;
            abilityClock = new SpecialBeat.Clock();
            SetState(ZombieState.Wander);
            CharacterVariety.Ensure(gameObject).Bind(string.IsNullOrEmpty(archetypeId) ? name : archetypeId, true);
        }

        public void SetAbility(ZombieSpecialAbility ability)
        {
            specialAbility = ability;
        }

        public void ApplyImpulse(Vector3 direction, float force, float stun)
        {
            if (currentState == ZombieState.Dead || healthSystem != null && healthSystem.IsDead) return;
            if (specialAbility == ZombieSpecialAbility.Charge) stun *= 0.3f;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = -transform.forward;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(transform.position + direction.normalized * Mathf.Clamp(force, 0.25f, 2.2f));
            }
            pendingStun = Mathf.Max(0.15f, stun);
            if (currentState == ZombieState.Stunned)
            {
                stateTimer = pendingStun;
                return;
            }
            SetState(ZombieState.Stunned);
        }

        private void OnEnable()
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.RegisterListener(this);
            }
        }

        private void Start()
        {
            CharacterVariety.Ensure(gameObject).Bind(string.IsNullOrEmpty(archetypeId) ? name : archetypeId, false);
            SetState(ZombieState.Wander);
        }

        private void OnDisable()
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.UnregisterListener(this);
            }
        }

        private void Update()
        {
            if (currentState == ZombieState.Dead || healthSystem.IsDead) return;
            if (GameManager.Instance != null)
            {
                var gameState = GameManager.Instance.CurrentState;
                if (gameState != GameState.ExpeditionActive && gameState != GameState.RaidActive) return;
            }

            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack && QualityProfile.SightDue(sightToken, Time.frameCount))
            {
                CheckSight();
            }
            if (agent != null && PlayerRegistry.Current != null)
            {
                float reach = Vector3.Distance(transform.position, PlayerRegistry.Current.transform.position);
                agent.obstacleAvoidanceType = reach > 18f
                    ? ObstacleAvoidanceType.LowQualityObstacleAvoidance
                    : ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }

            switch (currentState)
            {
                case ZombieState.Idle:
                    UpdateIdle();
                    break;
                case ZombieState.Wander:
                    UpdateWander();
                    break;
                case ZombieState.InvestigateNoise:
                    UpdateInvestigate();
                    break;
                case ZombieState.Chase:
                    UpdateChase();
                    break;
                case ZombieState.Attack:
                    UpdateAttack();
                    break;
                case ZombieState.Stunned:
                    UpdateStunned();
                    break;
                case ZombieState.Searching:
                    UpdateSearching();
                    break;
            }
        }

        private void SetState(ZombieState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            stateTimer = 0f;

            switch (newState)
            {
                case ZombieState.Idle:
                    agent.isStopped = true;
                    stateTimer = Random.Range(minWaitAtWanderPoint, maxWaitAtWanderPoint);
                    break;

                case ZombieState.Wander:
                    agent.isStopped = false;
                    agent.speed = wanderSpeed;
                    PickNewWanderDestination();
                    break;

                case ZombieState.InvestigateNoise:
                    agent.isStopped = false;
                    agent.speed = wanderSpeed * 1.3f;
                    agent.SetDestination(lastKnownPosition);
                    stateTimer = investigationDuration;
                    break;

                case ZombieState.Chase:
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    AlertNearbyZombies();
                    break;

                case ZombieState.Attack:
                    agent.isStopped = true;
                    abilityClock.Phase = 0;
                    abilityClock.Left = 0f;
                    break;

                case ZombieState.Stunned:
                    agent.isStopped = true;
                    stateTimer = pendingStun;
                    abilityClock.Phase = 0;
                    abilityClock.Left = 0f;
                    break;

                case ZombieState.Searching:
                    agent.isStopped = false;
                    agent.speed = wanderSpeed;
                    stateTimer = 5f;
                    PickSearchDestination();
                    break;

                case ZombieState.Dead:
                    agent.isStopped = true;
                    agent.enabled = false;
                    break;
            }
        }

        private void UpdateIdle()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                SetState(ZombieState.Wander);
            }
        }

        private void UpdateWander()
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                SetState(ZombieState.Idle);
            }
        }

        private void UpdateInvestigate()
        {
            stateTimer -= Time.deltaTime;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
            {
                if (stateTimer <= 0f)
                {
                    SetState(ZombieState.Searching);
                }
            }
        }

        private void UpdateChase()
        {
            if (currentTarget == null)
            {
                SetState(ZombieState.InvestigateNoise);
                return;
            }

            var targetHealth = currentTarget.GetComponent<IDamageable>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                currentTarget = null;
                SetState(ZombieState.Wander);
                return;
            }

            lastKnownPosition = currentTarget.position;
            agent.SetDestination(currentTarget.position);

            float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
            Vector3 flat = currentTarget.position - transform.position;
            flat.y = 0f;
            bool charging = specialAbility == ZombieSpecialAbility.Charge;
            bool lunging = specialAbility == ZombieSpecialAbility.Lunge;
            if (charging || lunging)
            {
                abilityClock = SpecialBeat.Advance(abilityClock, SpecialBeat.InReach(distToTarget, charging), Time.time, Time.deltaTime);
                if (abilityClock.Phase == 1)
                {
                    agent.isStopped = true;
                    if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat.normalized);
                }
                else if (abilityClock.Phase == 2)
                {
                    agent.isStopped = false;
                    agent.speed = 0f;
                    if (agent.isOnNavMesh && flat.sqrMagnitude > 0.01f)
                    {
                        agent.Move(flat.normalized * SpecialBeat.Speed(charging) * Time.deltaTime);
                    }
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                }

                if (SpecialBeat.Hits(abilityClock, distToTarget, attackRange + 0.35f))
                {
                    abilityClock.Struck = true;
                    abilityClock.Phase = 0;
                    abilityClock.Left = 0f;
                    abilityClock.Ready = Time.time + SpecialBeat.Cooldown;
                    ConnectDash(charging);
                }
            }

            if (distToTarget <= attackRange && abilityClock.Phase != 1)
            {
                SetState(ZombieState.Attack);
            }
        }

        private void ConnectDash(bool charge)
        {
            if (currentTarget == null) return;
            var damageable = currentTarget.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;
            float amount = charge ? attackDamage + 8f : SpecialBeat.LungeDamage;
            damageable.TakeDamage(amount, currentTarget.position, transform.forward, gameObject);
            if (charge) currentTarget.GetComponent<StatusEffectController>()?.Knockdown(0.7f);
        }

        private void UpdateSearching()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) SetState(ZombieState.Wander);
        }

        private void PickSearchDestination()
        {
            Vector3 offset = Random.insideUnitSphere * 4f;
            offset.y = 0f;
            if (NavMesh.SamplePosition(lastKnownPosition + offset, out NavMeshHit navHit, 4f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
        }

        private void UpdateAttack()
        {
            if (currentTarget == null)
            {
                SetState(ZombieState.Wander);
                return;
            }

            // Face target
            Vector3 lookDir = (currentTarget.position - transform.position);
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
            }

            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist > attackRange * 1.35f)
            {
                SetState(ZombieState.Chase);
                return;
            }

            if (Time.time >= nextAttackTime)
            {
                PerformBiteAttack();
            }
        }

        private void PerformBiteAttack()
        {
            nextAttackTime = Time.time + attackCooldown;

            var damageable = currentTarget.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(attackDamage, currentTarget.position, transform.forward, gameObject);
                var effects = currentTarget.GetComponent<StatusEffectController>();
                if (effects != null)
                {
                    if (Random.value < 0.35f) effects.ApplyBleed(5f);
                    if (Random.value < 0.2f) effects.ApplyInfection(8f);
                    if (specialAbility == ZombieSpecialAbility.Charge) effects.Knockdown(0.7f);
                }
            }
        }

        private void UpdateStunned()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                SetState(currentTarget != null ? ZombieState.Chase : ZombieState.Wander);
            }
        }

        private void CheckSight()
        {
            if (currentState == ZombieState.Chase || currentState == ZombieState.Attack) return;

            // Look for Player
            var player = PlayerRegistry.Current;
            if (player == null) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetEyePos = player.transform.position + Vector3.up * 1.5f;
            Vector3 dirToTarget = (targetEyePos - eyePos);
            float dist = dirToTarget.magnitude;

            // Player crouching reduces effective detection distance
            var visibility = player.GetComponent<PlayerVisibility>();
            float exposure = visibility != null ? visibility.Exposure : 0.65f;
            float effectiveSightRange = sightRange * Mathf.Lerp(0.35f, 1.2f, exposure);
            if (player.IsCrouching) effectiveSightRange *= 0.75f;
            effectiveSightRange *= WeatherController.SightMultiplier;
            effectiveSightRange *= OutpostZero.Expedition.CoverPost.ScaleFor(player.transform.position, transform.position, player.IsCrouching);

            if (dist <= effectiveSightRange)
            {
                float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
                if (angle <= sightAngle * 0.5f)
                {
                    Vector3 chest = player.transform.position + Vector3.up * 1.0f;
                    bool headBlocked = Physics.Raycast(eyePos, dirToTarget.normalized, dist, visionMask);
                    Vector3 toChest = chest - eyePos;
                    bool chestBlocked = Physics.Raycast(eyePos, toChest.normalized, toChest.magnitude, visionMask);
                    if (!headBlocked || !chestBlocked)
                    {
                        currentTarget = player.transform;
                        SetState(ZombieState.Chase);
                    }
                }
            }
        }

        public void OnHearNoise(Vector3 origin, float radius, float intensity, NoiseType noiseType, GameObject source)
        {
            if (currentState == ZombieState.Dead) return;

            // If already chasing, only redirect if much closer sound
            if (currentState == ZombieState.Chase && currentTarget != null) return;

            lastKnownPosition = origin;

            if (source != null && source.CompareTag("Player"))
            {
                currentTarget = source.transform;
                SetState(ZombieState.Chase);
            }
            else
            {
                SetState(ZombieState.InvestigateNoise);
            }
        }

        private void PickNewWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnOrigin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
        }

        private void AlertNearbyZombies()
        {
            if (NoiseManager.Instance == null || currentTarget == null) return;
            NoiseManager.Instance.EmitNoise(transform.position + Vector3.up * 1.2f, hordeAlertRadius, 1f, NoiseType.ZombieScream, currentTarget.gameObject);
        }

        private void HandleDamaged(float amount, Vector3 hitPoint)
        {
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack)
            {
                // Turn around towards damage source
                var player = PlayerRegistry.Current;
                if (player != null)
                {
                    currentTarget = player.transform;
                    SetState(ZombieState.Chase);
                }
            }
        }

        private void HandleDeath(Vector3 hitPoint, Vector3 hitDir, GameObject killer)
        {
            SetState(ZombieState.Dead);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecordZombieKill(archetypeId);
            }

            if (Random.value < 0.45f)
            {
                LootPickup.Spawn(LootKind.Scrap, Random.Range(2, 6), transform.position);
            }

            var pool = ZombiePool.Instance;
            if (pool != null)
            {
                pool.Release(gameObject, 4f);
            }
            else
            {
                Destroy(gameObject, 4f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, sightRange);
        }
    }
}

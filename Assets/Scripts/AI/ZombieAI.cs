using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Combat;

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
            Dead
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
        private Vector3 spawnOrigin;

        public Vector3 Position => transform.position;
        public float HearingSensitivity => hearingSensitivity;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            healthSystem = GetComponent<HealthSystem>();
            spawnOrigin = transform.position;

            healthSystem.OnDeath += HandleDeath;
            healthSystem.OnDamaged += HandleDamaged;
        }

        private void Start()
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.RegisterListener(this);
            }

            SetState(ZombieState.Wander);
        }

        private void OnDestroy()
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.UnregisterListener(this);
            }
        }

        private void Update()
        {
            if (currentState == ZombieState.Dead || healthSystem.IsDead) return;

            // Continually check visual cone for player/survivors
            CheckSight();

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
                    break;

                case ZombieState.Stunned:
                    agent.isStopped = true;
                    stateTimer = 0.8f;
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
                    SetState(ZombieState.Wander);
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
            if (distToTarget <= attackRange)
            {
                SetState(ZombieState.Attack);
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
            var player = FindObjectOfType<Player.PlayerController>();
            if (player == null) return;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 targetEyePos = player.transform.position + Vector3.up * 1.5f;
            Vector3 dirToTarget = (targetEyePos - eyePos);
            float dist = dirToTarget.magnitude;

            // Player crouching reduces effective detection distance
            float effectiveSightRange = player.IsCrouching ? sightRange * 0.55f : sightRange;

            if (dist <= effectiveSightRange)
            {
                float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
                if (angle <= sightAngle * 0.5f)
                {
                    // Line of sight raycast
                    if (!Physics.Raycast(eyePos, dirToTarget.normalized, dist, visionMask))
                    {
                        // Target acquired visually!
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
            Collider[] hits = Physics.OverlapSphere(transform.position, hordeAlertRadius);
            foreach (var hit in hits)
            {
                var otherZombie = hit.GetComponent<ZombieAI>();
                if (otherZombie != null && otherZombie != this && otherZombie.CurrentState == ZombieState.Wander)
                {
                    otherZombie.currentTarget = this.currentTarget;
                    otherZombie.SetState(ZombieState.Chase);
                }
            }
        }

        private void HandleDamaged(float amount, Vector3 hitPoint)
        {
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack)
            {
                // Turn around towards damage source
                var player = FindObjectOfType<Player.PlayerController>();
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
                GameManager.Instance.RecordZombieKill();
            }

            // Drop scrap or loot chance
            if (Random.value < 0.45f)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScrap(Random.Range(2, 6));
                }
            }

            Destroy(gameObject, 4f); // Clean up corpse after delay
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

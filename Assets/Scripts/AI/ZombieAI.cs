using System.Collections;
using System.Collections.Generic;
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
        private float swing = -1f;
        private float stateTimer = 0f;
        private float pendingStun = 0.8f;
        private SpecialBeat.Clock abilityClock;
        private float lostSight;
        private SearchMemory.Sweep searchSweep;
        private float dashX;
        private float dashZ = 1f;
        private Vector3 spawnOrigin;
        private static readonly List<ZombieAI> aliveCrowd = new List<ZombieAI>();
        private static readonly float[] crowdX = new float[48];
        private static readonly float[] crowdZ = new float[48];
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
            lostSight = 0f;
            searchSweep = new SearchMemory.Sweep();
            dashX = 0f;
            dashZ = 1f;
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
            if (!aliveCrowd.Contains(this)) aliveCrowd.Add(this);
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
            aliveCrowd.Remove(this);
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

            Separate();
        }

        private void Separate()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            if (abilityClock.Phase == 2 || currentState == ZombieState.Attack) return;
            int count = 0;
            for (int i = 0; i < aliveCrowd.Count && count < crowdX.Length; i++)
            {
                var other = aliveCrowd[i];
                if (other == null || other == this || other.currentState == ZombieState.Dead) continue;
                crowdX[count] = other.transform.position.x;
                crowdZ[count] = other.transform.position.z;
                count++;
            }
            agent.avoidancePriority = CrowdSpace.Priority(CrowdSpace.Neighbors(transform.position.x, transform.position.z, crowdX, crowdZ, count));
            CrowdSpace.Push(transform.position.x, transform.position.z, crowdX, crowdZ, count, out float pushX, out float pushZ);
            if (pushX * pushX + pushZ * pushZ < 0.0001f) return;
            agent.Move(new Vector3(pushX, 0f, pushZ) * CrowdSpace.Slide * Time.deltaTime);
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
                    swing = -1f;
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
                    searchSweep = SearchMemory.Start();
                    GoToSearchPoint();
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
            bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
            if (arrived || stateTimer <= 0f)
            {
                SetState(ZombieState.Searching);
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

            bool seen = StillSees(currentTarget);
            lostSight = SearchMemory.Lose(lostSight, seen, Time.deltaTime);
            if (SearchMemory.Forgotten(lostSight))
            {
                currentTarget = null;
                lostSight = 0f;
                SetState(ZombieState.InvestigateNoise);
                return;
            }
            if (seen) lastKnownPosition = currentTarget.position;

            float distToTarget = Vector3.Distance(transform.position, currentTarget.position);
            Vector3 aim = (seen ? currentTarget.position : lastKnownPosition) - transform.position;
            aim.y = 0f;
            bool charging = specialAbility == ZombieSpecialAbility.Charge;
            bool lunging = specialAbility == ZombieSpecialAbility.Lunge;
            int phaseBefore = abilityClock.Phase;
            if (charging || lunging)
            {
                abilityClock = SpecialBeat.Advance(abilityClock, SpecialBeat.InReach(distToTarget, charging), Time.time, Time.deltaTime, charging);
                if (phaseBefore != 2 && abilityClock.Phase == 2)
                {
                    SpecialBeat.Commit(aim.x, aim.z, out dashX, out dashZ);
                }
                if (abilityClock.Phase == 1)
                {
                    agent.isStopped = true;
                    if (aim.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(aim.normalized);
                }
                else if (abilityClock.Phase == 2)
                {
                    agent.isStopped = false;
                    agent.speed = 0f;
                    var dash = new Vector3(dashX, 0f, dashZ);
                    if (agent.isOnNavMesh && dash.sqrMagnitude > 0.01f)
                    {
                        agent.Move(dash * SpecialBeat.Speed(charging) * Time.deltaTime);
                    }
                    if (charging)
                    {
                        BraceCharge(dash);
                        if (currentState != ZombieState.Chase) return;
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

            if (abilityClock.Phase != 2)
            {
                agent.SetDestination(seen ? currentTarget.position : lastKnownPosition);
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
            bool arrived = agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.45f;
            int before = searchSweep.Index;
            searchSweep = SearchMemory.Tick(searchSweep, arrived, Time.deltaTime);
            if (SearchMemory.Done(searchSweep))
            {
                SetState(ZombieState.Wander);
                return;
            }
            if (searchSweep.Index != before) GoToSearchPoint();
        }

        private void GoToSearchPoint()
        {
            SearchMemory.Offset(searchSweep.Index, out float ox, out float oz);
            Vector3 spot = lastKnownPosition + new Vector3(ox, 0f, oz);
            if (NavMesh.SamplePosition(spot, out NavMeshHit navHit, SearchMemory.Radius, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
                return;
            }
            agent.SetDestination(spot);
        }

        private bool StillSees(Transform target)
        {
            if (target == null) return false;
            Vector3 eye = transform.position + Vector3.up * 1.5f;
            Vector3 head = target.position + Vector3.up * 1.5f;
            Vector3 toHead = head - eye;
            float dist = toHead.magnitude;
            if (dist > sightRange * 1.25f) return false;
            if (dist < 0.05f) return true;
            bool headBlocked = Physics.Raycast(eye, toHead.normalized, dist, visionMask);
            Vector3 chest = target.position + Vector3.up * 1.0f;
            Vector3 toChest = chest - eye;
            bool chestBlocked = toChest.sqrMagnitude < 0.01f || Physics.Raycast(eye, toChest.normalized, toChest.magnitude, visionMask);
            return !headBlocked || !chestBlocked;
        }

        private void BraceCharge(Vector3 dash)
        {
            if (dash.sqrMagnitude < 0.01f) return;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            float reach = SpecialBeat.Speed(true) * Time.deltaTime + 0.5f;
            if (!Physics.Raycast(origin, dash.normalized, out RaycastHit wall, reach, GameLayers.EnvironmentMask)) return;
            var board = wall.collider.GetComponentInParent<StreetBoard>();
            if (board != null)
            {
                board.Strike(BoardBreak.ChargeHit);
                if (board.Broken) return;
            }
            abilityClock.Phase = 0;
            abilityClock.Left = 0f;
            abilityClock.Ready = Time.time + SpecialBeat.Cooldown;
            ApplyImpulse(-dash, 0.4f, SpecialBeat.WallStun);
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

            if (swing < 0f)
            {
                if (Time.time < nextAttackTime) return;
                swing = 0f;
                nextAttackTime = Time.time + attackCooldown;
            }

            float before = swing;
            swing = SwingClock.Advance(swing, Time.deltaTime, attackCooldown);
            if (SwingClock.Connects(before, swing)) PerformBiteAttack();
            if (swing >= 1f) swing = -1f;
        }

        private void PerformBiteAttack()
        {

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
            float exposure = visibility != null
                ? visibility.Exposure
                : SpotRange.Exposure(player.IsCrouching, player.IsSprinting, player.FlashlightOn, 0f, 0f);
            float cover = OutpostZero.Expedition.CoverPost.ScaleFor(player.transform.position, transform.position, player.IsCrouching);
            float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
            Vector3 fromPlayer = transform.position - player.transform.position;
            fromPlayer.y = 0f;
            float beamAngle = Vector3.Angle(player.transform.forward, fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : player.transform.forward);
            bool inBeam = SpotRange.Beam(dist, beamAngle, player.FlashlightOn);
            bool inCone = SpotRange.Notices(dist, sightRange, exposure, player.IsCrouching, WeatherController.SightMultiplier, cover, angle, sightAngle);
            if (inCone || inBeam)
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

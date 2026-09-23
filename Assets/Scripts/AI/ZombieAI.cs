using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Combat;
using OutpostZero.Graphics;
using OutpostZero.Player;
using OutpostZero.Expedition;
using OutpostZero.Colony;
using OutpostZero.Items;

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

        /// <summary>FBX stem of this zombie's model; picks its Animator controller.</summary>
        public string ModelId { get; private set; } = "";

        /// <summary>Counts swings started, so the rig replays the attack clip per swing.</summary>
        public int Swings { get; private set; }

        /// <summary>Set by ZombieMotion when an imported attack clip will call <see cref="OnAttackImpact"/>.</summary>
        public bool ClipDriven { get; set; }

        public float ChaseSpeed => chaseSpeed;

        /// <summary>0 idle, 1 bracing for a lunge or charge, 2 dashing.</summary>
        public int AbilityPhase => abilityClock.Phase;

        public static void CountAlerts(float x, float z, out int bangs, out int questions)
        {
            bangs = 0;
            questions = 0;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || zombie.currentState == ZombieState.Dead) continue;
                float dx = zombie.transform.position.x - x;
                float dz = zombie.transform.position.z - z;
                if (!ThreatMark.Near((float)System.Math.Sqrt(dx * dx + dz * dz))) continue;
                string glyph = ThreatMark.Glyph(zombie.currentState);
                if (glyph == "!") bangs++;
                else if (glyph == "?") questions++;
            }
        }

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
        private int doorGen = -1;
        [SerializeField] private Vector3 lastKnownPosition;
        [SerializeField] private float investigationDuration = 6f;

        // Components
        private NavMeshAgent agent;
        private HealthSystem healthSystem;

        private float nextAttackTime = 0f;
        private float swing = -1f;
        private bool bitten;
        private float stateTimer = 0f;
        private float pendingStun = 0.8f;
        private SpecialBeat.Clock abilityClock;
        private float lostSight;
        private float lastDrip;
        private float limpLeft;
        private float whiffLeft;
        private float burnLeft;
        private Light emberLight;
        private SearchMemory.Sweep searchSweep;
        private float dashX;
        private float dashZ = 1f;
        private Vector3 spawnOrigin;
        private bool posted;
        private float postX;
        private float postZ;
        private float nextBite;
        private float nextGroan;
        private float nextPane;
        private bool fromRaid;
        private bool leaving;
        private static readonly List<ZombieAI> aliveCrowd = new List<ZombieAI>();
        private static readonly float[] crowdX = new float[48];
        private static readonly float[] crowdZ = new float[48];
        [SerializeField] private ZombieSpecialAbility specialAbility;
        private string archetypeId = "";
        private string lootTable = "";
        private int sightToken;

        public Vector3 Position => transform.position;
        public float HearingSensitivity => hearingSensitivity;

        public string WatchTarget => currentTarget != null ? currentTarget.name : "";
        public bool Posted => posted;

        private static int sightFrame = -1;
        private static int sightGroups = 1;

        private static int SightGroupsThisFrame()
        {
            int frame = Time.frameCount;
            if (frame != sightFrame)
            {
                sightFrame = frame;
                sightGroups = QualityProfile.SightGroups(AliveCount());
            }
            return sightGroups;
        }

        /// <summary>The closest live zombie to a point, or null. Allocation-free.</summary>
        public static ZombieAI NearestAlive(Vector3 from)
        {
            ZombieAI best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || zombie.currentState == ZombieState.Dead) continue;
                if (zombie.healthSystem != null && zombie.healthSystem.IsDead) continue;
                float sqr = (zombie.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = zombie;
                }
            }
            return best;
        }

        /// <summary>Enabled zombies that are not dead. Allocation-free, safe to read every frame.</summary>
        public static int AliveCount()
        {
            int count = 0;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || zombie.currentState == ZombieState.Dead) continue;
                if (zombie.healthSystem != null && zombie.healthSystem.IsDead) continue;
                count++;
            }
            return count;
        }

        public static int PostedCount()
        {
            int count = 0;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || !zombie.posted) continue;
                if (zombie.currentState == ZombieState.Dead) continue;
                if (zombie.healthSystem != null && zombie.healthSystem.IsDead) continue;
                count++;
            }
            return count;
        }

        public static float Nearest(float x, float z)
        {
            float best = -1f;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || zombie.currentState == ZombieState.Dead) continue;
                if (zombie.healthSystem != null && zombie.healthSystem.IsDead) continue;
                float dx = zombie.transform.position.x - x;
                float dz = zombie.transform.position.z - z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (best < 0f || dist < best) best = dist;
            }
            return best;
        }

        public static ZombieAI Closest(float x, float z, float reach)
        {
            if (reach < 0f) return null;
            ZombieAI bestBody = null;
            float best = reach;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || zombie.currentState == ZombieState.Dead) continue;
                if (zombie.healthSystem != null && zombie.healthSystem.IsDead) continue;
                float dx = zombie.transform.position.x - x;
                float dz = zombie.transform.position.z - z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > best) continue;
                best = dist;
                bestBody = zombie;
            }
            return bestBody;
        }

        public void PostAt(float x, float z)
        {
            posted = true;
            postX = x;
            postZ = z;
            nextBite = 0f;
        }

        public void MarkRaid()
        {
            fromRaid = true;
            leaving = false;
        }

        public static int RecallRaid()
        {
            int left = 0;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null) continue;
                bool dead = zombie.currentState == ZombieState.Dead || (zombie.healthSystem != null && zombie.healthSystem.IsDead);
                if (!RaidRecall.Leaves(zombie.fromRaid, dead)) continue;
                zombie.SendOff();
                left++;
            }
            return left;
        }

        private void SendOff()
        {
            fromRaid = false;
            leaving = true;
            posted = false;
            if (agent != null) agent.enabled = false;
            var pool = ZombiePool.Instance;
            if (pool != null) pool.Release(gameObject, 0.4f);
            else Destroy(gameObject);
        }

        private void ClearPost()
        {
            posted = false;
            if (agent != null && agent.enabled) agent.isStopped = false;
        }

        public float AbilityWait(float now)
        {
            float left = abilityClock.Ready - now;
            return left > 0f ? left : 0f;
        }

        public static int CopyWatch(string[] into, float now)
        {
            if (into == null) return 0;
            int n = 0;
            for (int i = 0; i < aliveCrowd.Count && n < into.Length; i++)
            {
                var zombie = aliveCrowd[i];
                if (zombie == null || zombie.currentState == ZombieState.Dead) continue;
                into[n++] = AiWatch.Line(zombie.currentState.ToString(), zombie.WatchTarget, zombie.AbilityWait(now), null);
            }
            return n;
        }

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

        public VfxEvent HitVfx { get; private set; } = VfxEvent.BloodSpray;
        public VfxEvent DeathVfx { get; private set; } = VfxEvent.DeathBurst;

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
            lootTable = archetype.lootTable ?? "";
            ModelId = CharacterRig.ModelId(archetype.modelPath);
            if (archetype.hitVfx != VfxEvent.None) HitVfx = archetype.hitVfx;
            if (archetype.deathVfx != VfxEvent.None) DeathVfx = archetype.deathVfx;
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
            Rise();
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
            posted = false;
            fromRaid = false;
            leaving = false;
            abilityClock = new SpecialBeat.Clock();
            lostSight = 0f;
            lastDrip = 0f;
            searchSweep = new SearchMemory.Sweep();
            limpLeft = 0f;
            whiffLeft = 0f;
            burnLeft = 0f;
            if (emberLight != null)
            {
                Destroy(emberLight.gameObject);
                emberLight = null;
            }
            dashX = 0f;
            dashZ = 1f;
            SetState(ZombieState.Wander);
            var melt = GetComponent<CorpseMelt>();
            if (melt != null) melt.Clear();
            CharacterVariety.Ensure(gameObject).Bind(string.IsNullOrEmpty(archetypeId) ? name : archetypeId, true);
        }

        public void SetAbility(ZombieSpecialAbility ability)
        {
            specialAbility = ability;
        }

        public void Ignite()
        {
            if (currentState == ZombieState.Dead || healthSystem != null && healthSystem.IsDead) return;
            burnLeft = Ember.Catch(burnLeft);
            ShowEmber();
        }

        public void Whiff(float x, float z)
        {
            if (currentState == ZombieState.Dead || healthSystem != null && healthSystem.IsDead) return;
            if (currentState == ZombieState.Chase || currentState == ZombieState.Attack || currentState == ZombieState.Stunned)
            {
                whiffLeft = WhiffClock.Seconds;
                return;
            }
            lastKnownPosition = new Vector3(x, transform.position.y, z);
            whiffLeft = WhiffClock.Seconds;
            SetState(ZombieState.InvestigateNoise);
        }

        public static void WhiffNear(float x0, float z0, float x1, float z1, ZombieAI struck)
        {
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var other = aliveCrowd[i];
                if (other == null || other == struck) continue;
                if (!WhiffClock.Passes(x0, z0, x1, z1, other.transform.position.x, other.transform.position.z, out float nearX, out float nearZ))
                    continue;
                other.Whiff(nearX, nearZ);
            }
        }

        public static void IgniteNear(float x, float z)
        {
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var other = aliveCrowd[i];
                if (other == null) continue;
                float dx = other.transform.position.x - x;
                float dz = other.transform.position.z - z;
                if (!Ember.Reaches(dx, dz)) continue;
                other.Ignite();
            }
        }

        private void TickEmber()
        {
            float before = burnLeft;
            burnLeft = Ember.Tick(burnLeft, OutpostZero.Graphics.RainQuench.Now(Time.deltaTime));
            ShowEmber();
            if (!Ember.Due(before, burnLeft) || healthSystem == null || healthSystem.IsDead) return;
            healthSystem.TakeDamage(Ember.Damage, transform.position + Vector3.up, Vector3.up, gameObject);
            OilPatch.Blast(transform.position);
            if (healthSystem.IsDead) return;
            for (int i = 0; i < aliveCrowd.Count; i++)
            {
                var other = aliveCrowd[i];
                if (other == null || other == this) continue;
                float dx = other.transform.position.x - transform.position.x;
                float dz = other.transform.position.z - transform.position.z;
                if (!Ember.Reaches(dx, dz)) continue;
                other.Ignite();
            }
        }

        private void ShowEmber()
        {
            if (burnLeft > 0f)
            {
                if (emberLight != null) return;
                var go = new GameObject("Ember");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                emberLight = go.AddComponent<Light>();
                emberLight.type = LightType.Point;
                emberLight.range = 3.5f;
                emberLight.intensity = 1.4f;
                emberLight.color = new Color(1f, 0.45f, 0.1f);
                return;
            }
            if (emberLight == null) return;
            Destroy(emberLight.gameObject);
            emberLight = null;
        }

        public void ApplyImpulse(Vector3 direction, float force, float stun)
        {
            if (currentState == ZombieState.Dead || healthSystem != null && healthSystem.IsDead) return;
            stun = HitStun.Resist(stun, specialAbility == ZombieSpecialAbility.Charge);
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = -transform.forward;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(transform.position + direction.normalized * Mathf.Clamp(force, 0.25f, 2.2f));
            }
            pendingStun = stun;
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
            if (leaving) return;
            if (currentState == ZombieState.Dead || healthSystem.IsDead) return;
            if (GameManager.Instance != null)
            {
                var gameState = GameManager.Instance.CurrentState;
                if (gameState != GameState.ExpeditionActive && gameState != GameState.RaidActive) return;
            }
            limpLeft = LimbCut.Tick(limpLeft, Time.deltaTime);
            whiffLeft = WhiffClock.Tick(whiffLeft, Time.deltaTime);
            TickEmber();
            Drip();

            if (posted)
            {
                if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.RaidActive || !Gnaw())
                    ClearPost();
                else
                    return;
            }
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack && QualityProfile.SightDue(sightToken, Time.frameCount, SightGroupsThisFrame()))
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

            FollowDoor();

            if (TargetDrop.Gone(currentTarget != null, currentTarget != null && currentTarget.gameObject.activeInHierarchy))
            {
                currentTarget = null;
                if (currentState == ZombieState.Chase || currentState == ZombieState.Attack)
                    SetState(ZombieState.Wander);
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
            ApplyLimp();
        }

        private void ApplyLimp()
        {
            if (agent == null || abilityClock.Phase == 2) return;
            float speed = GaitSpeed();
            if (speed <= 0f) return;
            agent.speed = GaitPace(speed);
        }

        private float GaitPace(float speed)
        {
            float paced = LimbCut.Speed(speed, limpLeft > 0f, specialAbility == ZombieSpecialAbility.Charge);
            paced = GasCloud.Speed(paced, GasField.Covers(transform.position.x, transform.position.z));
            paced = WhiffClock.Speed(paced, whiffLeft > 0f);
            return StreetSlick.Speed(paced, OilPatch.Covers(transform.position.x, transform.position.z));
        }

        private float GaitSpeed()
        {
            if (currentState == ZombieState.Chase) return chaseSpeed;
            if (currentState == ZombieState.InvestigateNoise) return wanderSpeed * 1.3f;
            if (currentState == ZombieState.Wander || currentState == ZombieState.Searching) return wanderSpeed;
            return 0f;
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
            Voice("idle");
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                SetState(ZombieState.Wander);
            }
        }

        private void UpdateWander()
        {
            Voice("idle");
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

        private void FollowDoor()
        {
            if (currentState != ZombieState.Chase && currentState != ZombieState.Attack) return;
            if (!DoorCross.ShouldFollow(doorGen, transform.position.x, transform.position.z, Time.time, out int generation)) return;
            doorGen = generation;
            DoorCross.Slot(GetInstanceID() & 0x7fffffff, out float ox, out float oz);
            var dest = new Vector3(DoorCross.ToX + ox, transform.position.y, DoorCross.ToZ + oz);
            bool placed = agent != null && agent.isOnNavMesh && agent.Warp(dest);
            if (!placed)
            {
                if (agent != null) agent.enabled = false;
                transform.position = dest;
                if (agent != null)
                {
                    agent.enabled = true;
                    if (agent.isOnNavMesh) agent.Warp(dest);
                }
            }
            lastKnownPosition = dest;
        }

        private bool Gnaw()
        {
            if (GridBuilder.Instance == null || !GridBuilder.Instance.BoardStands(postX, postZ))
            {
                ClearPost();
                return false;
            }
            Vector3 spot = new Vector3(postX, transform.position.y, postZ);
            if (!BoardBite.InReach(transform.position.x, transform.position.z, postX, postZ))
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(spot);
                }
                else
                {
                    Vector3 step = spot - transform.position;
                    step.y = 0f;
                    if (step.sqrMagnitude > 0.01f)
                        transform.position += step.normalized * GaitPace(chaseSpeed) * Time.deltaTime;
                }
                return true;
            }
            if (agent != null && agent.enabled) agent.isStopped = true;
            if (Time.time < nextBite) return true;
            nextBite = Time.time + BoardBite.Gap;
            if (GridBuilder.Instance.StrikeAt(postX, postZ, BoardBite.Chip))
            {
                SurvivorRoster.Instance?.WoundFromRaid(Mathf.RoundToInt(postX + postZ));
                ClearPost();
                return false;
            }
            return posted;
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
                RakeGlass(aim);
            }

            if (distToTarget <= attackRange && abilityClock.Phase != 1)
            {
                SetState(ZombieState.Attack);
            }
        }

        private void RakeGlass(Vector3 aim)
        {
            if (aim.sqrMagnitude < 0.01f || Time.time < nextPane) return;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            bool blocked = Physics.Raycast(origin, aim.normalized, out RaycastHit wall, PaneClaw.Reach, GameLayers.EnvironmentMask);
            if (blocked)
            {
                var pane = wall.collider.GetComponent<GlassPane>();
                if (pane != null)
                {
                    nextPane = Time.time + PaneClaw.Gap;
                    pane.TakeDamage(PaneClaw.Hit, wall.point, aim, gameObject);
                    return;
                }
            }
            if (!Physics.Raycast(origin, aim.normalized, out RaycastHit slab, BarClaw.Reach, GameLayers.InteractableMask)) return;
            if (blocked && wall.distance < slab.distance) return;
            var door = slab.collider.GetComponent<StreetDoor>();
            if (door == null || !door.Barred) return;
            nextPane = Time.time + BarClaw.Gap;
            door.Rake(gameObject);
        }

        private void ConnectDash(bool charge)
        {
            if (currentTarget == null) return;
            var damageable = currentTarget.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) return;
            float amount = charge ? attackDamage + 8f : SpecialBeat.LungeDamage;
            damageable.TakeDamage(amount, currentTarget.position, transform.forward, gameObject);
            if (charge) currentTarget.GetComponent<StatusEffectController>()?.Knockdown(0.7f);
            if (charge) ExpeditionCameraRig.At(CameraTuning.BruteStomp, transform.position);
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

        private static bool SightBlocked(Vector3 origin, Vector3 direction, float distance, LayerMask mask)
        {
            if (distance <= 0.05f) return false;
            var hits = Physics.RaycastAll(origin, direction, distance, mask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;
            var names = new string[hits.Length];
            for (int i = 0; i < hits.Length; i++)
                names[i] = hits[i].collider != null ? hits[i].collider.name : "";
            return PaneGlass.Occluded(names);
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
            if (SmokeMark.Hides(transform.position.x, transform.position.z, target.position.x, target.position.z))
                return false;
            bool headBlocked = SightBlocked(eye, toHead.normalized, dist, visionMask);
            Vector3 chest = target.position + Vector3.up * 1.0f;
            Vector3 toChest = chest - eye;
            bool chestBlocked = toChest.sqrMagnitude < 0.01f || SightBlocked(eye, toChest.normalized, toChest.magnitude, visionMask);
            return !headBlocked || !chestBlocked;
        }

        private void BraceCharge(Vector3 dash)
        {
            if (dash.sqrMagnitude < 0.01f) return;
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            float reach = SpecialBeat.Speed(true) * Time.deltaTime + 0.5f;
            bool wallHit = Physics.Raycast(origin, dash.normalized, out RaycastHit wall, reach, GameLayers.EnvironmentMask);
            if (RipBar(origin, dash, reach, wallHit, wall)) return;
            if (!wallHit) return;
            var board = wall.collider.GetComponentInParent<StreetBoard>();
            if (board != null)
            {
                board.Strike(BoardBreak.ChargeHit);
                if (board.Broken) return;
            }
            var pane = wall.collider.GetComponent<GlassPane>();
            if (pane != null)
            {
                float before = pane.Current;
                pane.TakeDamage(PaneCharge.Hit, wall.point, dash, gameObject);
                if (PaneCharge.Through(before, PaneCharge.Hit)) return;
            }
            abilityClock.Phase = 0;
            abilityClock.Left = 0f;
            abilityClock.Ready = Time.time + SpecialBeat.Cooldown;
            ApplyImpulse(-dash, 0.4f, SpecialBeat.WallStun);
        }

        private bool RipBar(Vector3 origin, Vector3 dash, float reach, bool wallHit, RaycastHit wall)
        {
            if (!Physics.Raycast(origin, dash.normalized, out RaycastHit slab, reach, GameLayers.InteractableMask)) return false;
            if (wallHit && wall.distance < slab.distance) return false;
            var door = slab.collider.GetComponent<StreetDoor>();
            if (door == null || !door.Barred) return false;
            door.Rip(gameObject);
            if (!door.Barred) return true;
            abilityClock.Phase = 0;
            abilityClock.Left = 0f;
            abilityClock.Ready = Time.time + SpecialBeat.Cooldown;
            ApplyImpulse(-dash, 0.4f, SpecialBeat.WallStun);
            return true;
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
                bitten = false;
                Swings++;
                nextAttackTime = Time.time + attackCooldown;
                Voice("bite");
            }

            float before = swing;
            swing = SwingClock.Advance(swing, Time.deltaTime, attackCooldown);
            if (SwingClock.Bites(before, swing, bitten, ClipDriven)) Bite();
            if (swing >= 1f) swing = -1f;
        }

        /// <summary>Animation event from the attack clip's contact frame.</summary>
        public void OnAttackImpact()
        {
            if (currentState != ZombieState.Attack || currentTarget == null) return;
            if (!SwingClock.Hears(swing, bitten)) return;
            if (Vector3.Distance(transform.position, currentTarget.position) > attackRange * 1.35f) return;
            Bite();
        }

        private void Bite()
        {
            bitten = true;
            PerformBiteAttack();
        }

        private void PerformBiteAttack()
        {
            var escort = currentTarget.GetComponent<RescueFollower>();
            if (escort != null)
            {
                escort.BiteFrom(Time.time);
                return;
            }

            var damageable = currentTarget.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(attackDamage, currentTarget.position, transform.forward, gameObject);
                var effects = currentTarget.GetComponent<StatusEffectController>();
                if (effects != null)
                {
                    bool runner = specialAbility == ZombieSpecialAbility.Lunge;
                    if (ClawCut.Opens(runner, Random.value)) effects.ApplyBleed(5f);
                    if (ClawCut.Infects(Random.value)) effects.ApplyInfection(8f);
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
            float bare = visibility != null
                ? visibility.Exposure
                : SpotRange.Exposure(player.IsCrouching, player.IsSprinting, player.FlashlightOn, 0f, 0f);
            float exposure = visibility != null
                ? bare
                : RailLamp.Exposure(bare, player.RailLit, player.FlashlightOn);
            float cover = OutpostZero.Expedition.CoverPost.ScaleFor(player.transform.position, transform.position, player.IsCrouching);
            float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);
            Vector3 fromPlayer = transform.position - player.transform.position;
            fromPlayer.y = 0f;
            float beamAngle = Vector3.Angle(player.transform.forward, fromPlayer.sqrMagnitude > 0.001f ? fromPlayer.normalized : player.transform.forward);
            bool inBeam = SpotRange.Beam(dist, beamAngle, player.FlashlightOn)
                || RailLamp.Beam(dist, beamAngle, player.RailLit);
            bool inCone = SpotRange.Notices(dist, sightRange, exposure, player.IsCrouching, WeatherController.SightMultiplier, cover, angle, sightAngle);
            if (inCone || inBeam)
            {
                if (SmokeMark.Hides(transform.position.x, transform.position.z, player.transform.position.x, player.transform.position.z))
                    return;
                Vector3 chest = player.transform.position + Vector3.up * 1.0f;
                bool headBlocked = SightBlocked(eyePos, dirToTarget.normalized, dist, visionMask);
                Vector3 toChest = chest - eyePos;
                bool chestBlocked = SightBlocked(eyePos, toChest.normalized, toChest.magnitude, visionMask);
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

            bool chasing = currentState == ZombieState.Chase && currentTarget != null;
            var escort = source != null ? source.GetComponent<RescueFollower>() : null;
            if (FollowPull.Chases(escort != null, escort != null && escort.Following, chasing, noiseType))
            {
                lastKnownPosition = origin;
                currentTarget = source.transform;
                SetState(ZombieState.Chase);
                return;
            }

            // If already chasing, only redirect if much closer sound
            if (chasing) return;

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
            NoiseManager.Instance.EmitNoise(transform.position, hordeAlertRadius, 1f, NoiseType.ZombieScream, currentTarget.gameObject);
        }

        private void HandleDamaged(float amount, Vector3 hitPoint)
        {
            if (LimbCut.Leg(hitPoint.y, transform.position.y)) limpLeft = LimbCut.Seconds;
            Voice("hurt");
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
            Voice("death");
            SetState(ZombieState.Dead);
            Collapse();
            CombatVfx.Play(DeathVfx, transform.position + Vector3.up, hitDir);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecordZombieKill(archetypeId);
            }

            DropLoot();

            var melt = GetComponent<CorpseMelt>();
            if (melt == null) melt = gameObject.AddComponent<CorpseMelt>();
            melt.Begin();
            var pool = ZombiePool.Instance;
            if (pool != null)
            {
                pool.Release(gameObject, CorpseMelt.Length);
            }
            else
            {
                Destroy(gameObject, CorpseMelt.Length);
            }
        }

        private void DropLoot()
        {
            if (string.IsNullOrEmpty(lootTable))
            {
                if (Random.value < 0.45f) LootPickup.Spawn(LootKind.Scrap, Random.Range(2, 6), transform.position);
                return;
            }
            var drops = LootTables.Drops(lootTable, GetInstanceID() ^ Time.frameCount);
            for (int i = 0; i < drops.Length; i++)
            {
                var spot = transform.position + Quaternion.Euler(0f, i * 137f, 0f) * Vector3.forward * 0.5f;
                if (drops[i].ItemId == "scrap") LootPickup.Spawn(LootKind.Scrap, drops[i].Count, spot);
                else ItemDatabase.SpawnWorld(drops[i].ItemId, drops[i].Count, spot + Vector3.up * 0.15f, Quaternion.Euler(0f, i * 61f, 0f));
            }
        }

        /// <summary>
        /// The body stops blocking shots and walkers. A blast kill throws it as a ragdoll on the
        /// Corpse layer; anything else plays its death clip where it stands.
        /// </summary>
        private void Collapse()
        {
            foreach (var solid in GetComponents<Collider>()) solid.enabled = false;
            if (!BlastKill.Active) return;
            float scale = transform.lossyScale.y;
            float mass = RagdollSheet.MassOf(specialAbility == ZombieSpecialAbility.Charge, scale);
            Ragdoll.Ensure(gameObject).Fall(BlastKill.PushFor(transform.position) * (RagdollSheet.BodyMass / mass), mass);
        }

        private void Rise()
        {
            foreach (var solid in GetComponents<Collider>()) solid.enabled = true;
            if (TryGetComponent(out Ragdoll limp)) limp.Stand();
            if (TryGetComponent(out ZombieMotion motion)) motion.Restart();
        }

        private void Drip()
        {
            if (healthSystem == null) return;
            int gore = SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;
            if (!WoundShow.Bleeds(gore)) return;
            if (!CharacterLook.Wounded(healthSystem.CurrentHealth, healthSystem.MaxHealth, gore)) return;
            if (!WoundShow.DripDue(Time.time, lastDrip)) return;
            lastDrip = Time.time;
            CombatVfx.Drip(transform.position);
        }

        private string Breed()
        {
            int ability = specialAbility == ZombieSpecialAbility.Charge ? 2 : specialAbility == ZombieSpecialAbility.Lunge ? 1 : 0;
            string id = string.IsNullOrEmpty(archetypeId) ? name : archetypeId;
            return ZombieVoice.Breed(id, ability);
        }

        private void Voice(string moment)
        {
            string breed = Breed();
            if (moment == "bite" && breed == "brute") ExpeditionCameraRig.At(CameraTuning.BruteStomp, transform.position);
            var ear = OutpostZero.Shell.AudioManager.Instance;
            if (ear == null) return;
            if (moment == "idle")
            {
                ear.Groan(breed, transform.position, Time.time, nextGroan, out nextGroan);
                return;
            }
            string id = moment == "bite" ? ZombieVoice.Bite(breed) : moment == "hurt" ? ZombieVoice.Hurt(breed) : ZombieVoice.Death(breed);
            float volume = breed == "brute" ? 0.72f : 0.48f;
            float pitch = breed == "brute" ? 0.55f : breed == "runner" ? 1.12f : 0f;
            ear.PlayAt(id, transform.position, volume, pitch);
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

using System;
using System.Text;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Combat;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HealthSystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float crouchSpeed = 2.2f;
        [SerializeField] private float rotationSpeed = 15f;

        [Header("Stamina System")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float staminaDrainRate = 22f; // Per second sprinting
        [SerializeField] private float staminaRegenRate = 16f; // Per second resting
        [SerializeField] private float staminaRegenDelay = 1.2f;
        private float lastStaminaDrainTime;

        [Header("Footstep Noise Footprint")]
        [SerializeField] private float walkNoiseRadius = 6f;
        [SerializeField] private float sprintNoiseRadius = 13f;
        [SerializeField] private float crouchNoiseRadius = 2f;
        [SerializeField] private float footstepInterval = 0.45f;
        private float nextFootstepTime;

        [Header("Weapons & Aiming")]
        [SerializeField] private WeaponBase[] equippedWeapons;
        [SerializeField] private int activeWeaponIndex = 0;
        [SerializeField] private Transform weaponHoldPoint;
        [SerializeField] private LayerMask groundAimMask;

        [Header("Tactical Equipment")]
        [SerializeField] private Light flashlight;
        [SerializeField] private bool flashlightOn = false;
        [SerializeField] private float lampCell = LampCell.Full;

        // Components
        private CharacterController characterController;
        private HealthSystem healthSystem;
        private PlayerInventory inventory;
        private Camera mainCamera;

        // State Flags
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsAimingDownSights { get; private set; }
        private bool sprintLatch;
        public bool FlashlightOn => flashlightOn;
        public float LampCellCharge => lampCell;
        public float LampSpent => LampCell.Spent(lampCell);
        public bool WheelOpen { get; private set; }
        public int WheelSlot { get; private set; } = -1;
        private float wheelHold;
        private float lastDodge = -100f;
        private float dodgeUntil;
        private Vector3 dodgeDir;
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public WeaponBase ActiveWeapon => (equippedWeapons != null && equippedWeapons.Length > activeWeaponIndex) ? equippedWeapons[activeWeaponIndex] : null;

        public event Action<float, float> OnStaminaChanged; // current, max
        public event Action<WeaponBase> OnActiveWeaponChanged;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            healthSystem = GetComponent<HealthSystem>();
            inventory = GetComponent<PlayerInventory>();
            mainCamera = Camera.main;
            currentStamina = maxStamina;

            healthSystem.OnDeath += HandlePlayerDeath;
            healthSystem.OnDamaged += HandleHurt;
            GameLayers.ApplyRecursively(gameObject, GameLayers.Player);
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
        }

        public string PackMods()
        {
            if (equippedWeapons == null || equippedWeapons.Length == 0) return "";
            var slots = new string[equippedWeapons.Length];
            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                var mod = equippedWeapons[i] != null ? equippedWeapons[i].GetComponent<WeaponMod>() : null;
                slots[i] = mod != null ? mod.Pack() : "";
            }
            return WeaponMod.JoinSlots(slots);
        }

        public void RestoreMods(string packed)
        {
            if (equippedWeapons == null) return;
            string[] slots = WeaponMod.SplitSlots(packed);
            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] == null) continue;
                string slot = i < slots.Length ? slots[i] : "";
                var mod = equippedWeapons[i].GetComponent<WeaponMod>();
                if (mod == null)
                {
                    if (string.IsNullOrEmpty(slot)) continue;
                    mod = equippedWeapons[i].gameObject.AddComponent<WeaponMod>();
                }
                mod.Restore(slot);
            }
        }

        public void Configure(WeaponBase[] weapons, Light tacticalLight)
        {
            equippedWeapons = weapons;
            flashlight = tacticalLight;
            groundAimMask = GameLayers.EnvironmentMask | 1;
        }

        public bool TakeFromGround(string weaponId, int magazine, int reserve, out string leftId, out int leftMag, out int leftReserve)
        {
            leftId = "";
            leftMag = 0;
            leftReserve = 0;
            var spec = WeaponCard.Find(weaponId);
            if (string.IsNullOrEmpty(spec.Id)) return false;

            int index = IndexOfType(spec.Type);
            if (index >= 0 && equippedWeapons[index] is FirearmWeapon carried)
            {
                leftId = carried.CardId;
                leftMag = carried.CurrentAmmo;
                leftReserve = carried.ReserveAmmo;
                carried.LoadCard(spec, magazine, reserve);
                SelectWeapon(index);
                return true;
            }
            if (index >= 0) return false;

            var created = SpawnWeapon(spec, magazine, reserve);
            if (created == null) return false;
            AddWeapon(created);
            SelectWeapon(equippedWeapons.Length - 1);
            return true;
        }

        private int IndexOfType(WeaponType type)
        {
            if (equippedWeapons == null) return -1;
            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] != null && equippedWeapons[i].Type == type) return i;
            }
            return -1;
        }

        private WeaponBase SpawnWeapon(WeaponCard.Spec spec, int magazine, int reserve)
        {
            Transform socket = transform.Find("Weapon_Socket");
            if (socket == null) socket = transform;
            var weaponObject = new GameObject(spec.Id);
            weaponObject.transform.SetParent(socket, false);
            if (spec.Melee)
            {
                var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                definition.id = spec.Id;
                definition.displayName = spec.Name;
                definition.weaponType = spec.Type;
                definition.baseDamage = spec.Damage;
                definition.attackRate = spec.Rate;
                definition.range = spec.Range;
                definition.noiseRadius = spec.Noise;
                definition.noiseType = spec.NoiseKind;
                definition.isMelee = true;
                var melee = weaponObject.AddComponent<MeleeWeapon>();
                melee.Configure(definition);
                return melee;
            }
            var gun = weaponObject.AddComponent<FirearmWeapon>();
            gun.LoadCard(spec, magazine, reserve);
            return gun;
        }

        public void AddWeapon(WeaponBase weapon)
        {
            if (weapon == null) return;
            var list = new System.Collections.Generic.List<WeaponBase>();
            if (equippedWeapons != null) list.AddRange(equippedWeapons);
            list.Add(weapon);
            equippedWeapons = list.ToArray();
            weapon.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (equippedWeapons != null && equippedWeapons.Length > 0)
            {
                SelectWeapon(0);
            }

            ApplyLamp();
        }

        public void RestoreLamp(float spent)
        {
            lampCell = LampCell.FromSpent(spent);
            if (!LampCell.Live(lampCell)) flashlightOn = false;
            ApplyLamp();
        }

        private void ApplyLamp()
        {
            if (flashlight == null) return;
            bool shine = flashlightOn && LampCell.Live(lampCell);
            flashlight.enabled = shine;
            if (shine) flashlight.intensity = LampCell.Intensity(lampCell);
        }

        private void Update()
        {
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state != GameState.ExpeditionActive && state != GameState.RaidActive && state != GameState.CampManagement)
                {
                    return;
                }
            }

            if (healthSystem.IsDead) return;

            HandleInput();
            lampCell = LampCell.Tick(lampCell, flashlightOn, Time.deltaTime);
            if (flashlightOn && !LampCell.Live(lampCell))
            {
                flashlightOn = false;
                AudioManager.Instance?.Play("clack");
            }
            ApplyLamp();
            healthSystem.Shielded = DodgeClock.Untouchable(Time.time - lastDodge);
            HandleAiming();
            HandleMovement();
            HandleStamina();
            HandleWeapons();
        }

        private void HandleInput()
        {
            // Flashlight Toggle (F)
            if (ExpeditionInput.FlashlightPressed)
            {
                if (!flashlightOn && !LampCell.Live(lampCell))
                {
                    AudioManager.Instance?.Play("clack");
                }
                else
                {
                    flashlightOn = !flashlightOn;
                    if (flashlightOn) CodexDirector.Hear("flashlight");
                }
                ApplyLamp();
            }

            if (ExpeditionInput.MedkitPressed)
            {
                if (inventory != null && inventory.UseMedkit())
                {
                    GameplayFeedback.Toast(FieldHand.Dose(inventory.LastDoseSkill));
                    CodexDirector.Hear("medkit");
                }
            }

            if (ExpeditionInput.ReloadPressed && ActiveWeapon is FirearmWeapon firearm)
            {
                firearm.TryStartReload();
                if (firearm.IsReloading) CodexDirector.Hear("reload");
            }

            IsAimingDownSights = ExpeditionInput.AimHeld && !WheelOpen && Time.time >= dodgeUntil;
            if (IsAimingDownSights) CodexDirector.Hear("aim");

            if (!TrackWheel())
            {
                for (int i = 0; i < 4; i++)
                {
                    if (ExpeditionInput.WeaponSlotPressed(i)) SelectWeapon(i);
                }
            }

            if (inventory != null)
            {
                for (int i = 0; i < ItemBelt.Count; i++)
                {
                    if (ExpeditionInput.BeltPressed(i)) inventory.UseBelt(i);
                }
            }

            TryDodge();
            if (WheelOpen) return;

            float scroll = ExpeditionInput.Scroll;
            if (scroll > 0.05f) CycleWeapon(1);
            else if (scroll < -0.05f) CycleWeapon(-1);
            int padCycle = ExpeditionInput.WeaponCycle;
            if (padCycle != 0) CycleWeapon(padCycle);
        }

        private bool TrackWheel()
        {
            bool held = ExpeditionInput.WheelHeld;
            if (held)
            {
                wheelHold += Time.deltaTime;
                if (!WeaponWheel.Shown(true, wheelHold)) return true;
                WheelOpen = true;
                ReadWheelPoint(out float x, out float y);
                int picked = WeaponWheel.Pick(x, y);
                if (picked >= 0) WheelSlot = picked;
                return true;
            }

            bool wasOpen = WheelOpen;
            int highlight = WheelSlot;
            float heldFor = wheelHold;
            wheelHold = 0f;
            WheelOpen = false;
            WheelSlot = -1;
            if (!wasOpen && !WeaponWheel.Shown(true, heldFor)) return false;

            int pick = WeaponWheel.Release(highlight, activeWeaponIndex);
            if (equippedWeapons != null && pick != activeWeaponIndex && pick >= 0 && pick < equippedWeapons.Length && equippedWeapons[pick] != null)
            {
                SelectWeapon(pick);
            }
            return true;
        }

        private void ReadWheelPoint(out float x, out float y)
        {
            Vector2 aim = ExpeditionInput.AimStick;
            x = aim.x;
            y = aim.y;
            if (x * x + y * y >= WeaponWheel.Deadzone * WeaponWheel.Deadzone) return;
            if (Screen.width > 1 && Screen.height > 1)
            {
                Vector2 pointer = ExpeditionInput.Pointer;
                float span = Screen.height * 0.5f;
                x = (pointer.x - Screen.width * 0.5f) / span;
                y = (pointer.y - Screen.height * 0.5f) / span;
                return;
            }
            Vector2 move = ExpeditionInput.Move;
            x = move.x;
            y = move.y;
        }

        public string WheelLine()
        {
            if (!WheelOpen || equippedWeapons == null) return "";
            var builder = new StringBuilder();
            int count = equippedWeapons.Length < WeaponWheel.Slots ? equippedWeapons.Length : WeaponWheel.Slots;
            for (int i = 0; i < count; i++)
            {
                if (i > 0) builder.Append('\n');
                string name = equippedWeapons[i] != null ? equippedWeapons[i].WeaponName : "";
                builder.Append(WeaponWheel.Row(i, name, i == WheelSlot));
            }
            return builder.ToString();
        }

        private void TryDodge()
        {
            if (Time.time < dodgeUntil || !ExpeditionInput.DodgePressed) return;
            var effects = GetComponent<StatusEffectController>();
            if (effects != null && effects.IsKnockedDown) return;
            if (!DodgeClock.Ready(currentStamina, Time.time - lastDodge)) return;

            Vector2 move = ExpeditionInput.Move;
            DodgeClock.Direction(move.x, move.y, transform.forward.x, transform.forward.z, out float dx, out float dz);
            dodgeDir = new Vector3(dx, 0f, dz);
            lastDodge = Time.time;
            dodgeUntil = Time.time + DodgeClock.Duration;
            healthSystem.Shielded = true;
            currentStamina = Mathf.Max(0f, currentStamina - DodgeClock.Cost);
            lastStaminaDrainTime = Time.time;
            IsAimingDownSights = false;
            IsSprinting = false;
            float cap = StaminaPool();
            OnStaminaChanged?.Invoke(currentStamina, cap);
        }

        private void HandleMovement()
        {
            var effects = GetComponent<StatusEffectController>();
            if (effects != null && effects.IsKnockedDown)
            {
                IsSprinting = false;
                characterController.Move(Vector3.down * Time.deltaTime);
                return;
            }

            var hands = GetComponent<PlayerInteractor>();
            if (hands != null && hands.TakingDown)
            {
                IsSprinting = false;
                characterController.Move(Vector3.down * Time.deltaTime);
                return;
            }

            if (Time.time < dodgeUntil)
            {
                IsSprinting = false;
                Vector3 dash = dodgeDir * DodgeClock.Speed;
                dash.y = characterController.isGrounded ? -0.5f : -9.81f;
                characterController.Move(dash * Time.deltaTime);
                return;
            }

            Vector2 move = ExpeditionInput.Move;
            Vector3 inputDirection = new Vector3(move.x, 0f, move.y);
            if (inputDirection.sqrMagnitude > 1f) inputDirection.Normalize();

            bool isMoving = inputDirection.sqrMagnitude > 0.01f;
            if (isMoving) CodexDirector.Hear("move");

            int crouchMode = SettingsService.Instance != null ? SettingsService.Instance.CrouchMode : 0;
            int sprintMode = SettingsService.Instance != null ? SettingsService.Instance.SprintMode : 0;
            IsCrouching = PlayOptions.Stance(ExpeditionInput.CrouchHeld, ExpeditionInput.CrouchPressed, IsCrouching, crouchMode);
            sprintLatch = PlayOptions.Stance(ExpeditionInput.SprintHeld, ExpeditionInput.SprintPressed, sprintLatch, sprintMode);
            if (IsCrouching) CodexDirector.Hear("crouch");
            if (IsSprinting) CodexDirector.Hear("sprint");
            bool wantsToSprint = sprintLatch && !IsCrouching && currentStamina > 5f;

            IsSprinting = isMoving && wantsToSprint;

            // Speed evaluation
            float currentSpeed = walkSpeed;
            if (IsCrouching) currentSpeed = crouchSpeed;
            else if (IsSprinting) currentSpeed = sprintSpeed * (effects != null ? effects.SprintBonus : 1f);
            if (inventory != null) currentSpeed *= Mathf.Lerp(1f, 0.72f, inventory.WeightRatio);
            if (effects != null) currentSpeed *= effects.SlowMultiplier;

            Vector3 moveVector = inputDirection * currentSpeed;

            // Simple gravity
            if (!characterController.isGrounded)
            {
                moveVector.y = -9.81f;
            }
            else
            {
                moveVector.y = -0.5f; // Keep grounded
            }

            characterController.Move(moveVector * Time.deltaTime);

            // Footstep noise generation
            if (isMoving && characterController.isGrounded && Time.time >= nextFootstepTime)
            {
                GenerateFootstepNoise();
            }
        }

        public void PlayFootstep()
        {
            if (characterController == null || !characterController.isGrounded) return;
            if (characterController.velocity.magnitude < 0.2f) return;
            GenerateFootstepNoise();
        }

        private void GenerateFootstepNoise()
        {
            float interval = footstepInterval;
            float radius = walkNoiseRadius;
            NoiseType nType = NoiseType.WalkFootstep;

            if (IsSprinting)
            {
                interval = footstepInterval * 0.65f;
                radius = sprintNoiseRadius;
                nType = NoiseType.SprintFootstep;
            }
            else if (IsCrouching)
            {
                interval = footstepInterval * 1.35f;
                radius = crouchNoiseRadius;
                nType = NoiseType.SneakFootstep;
            }

            nextFootstepTime = Time.time + interval;

            if (NoiseManager.Instance != null && radius > 0f)
            {
                NoiseManager.Instance.EmitNoise(transform.position, radius, 0.7f, nType, gameObject);
            }
        }

        private void HandleAiming()
        {
            bool invert = SettingsService.Instance != null && SettingsService.Instance.InvertLook;
            Vector2 aimStick = ExpeditionInput.AimStick;
            aimStick.y = PlayOptions.StickY(aimStick.y, invert);
            if (aimStick.sqrMagnitude > 0.04f)
            {
                Vector3 stickDir = new Vector3(aimStick.x, 0f, aimStick.y);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(stickDir), AimBlend());
                NudgeAim();
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null)
            {
                NudgeAim();
                return;
            }

            Vector2 pointer = ExpeditionInput.Pointer;
            if (invert && Screen.height > 1) pointer.y = Screen.height - pointer.y;
            Ray ray = mainCamera.ScreenPointToRay(pointer);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDirection = (hitPoint - transform.position);
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, AimBlend());
                }
            }
            NudgeAim();
        }

        private float AimBlend()
        {
            var needs = GetComponent<SurvivalNeeds>();
            float scale = needs != null ? needs.AimScale : 1f;
            return rotationSpeed * Time.deltaTime * scale;
        }

        private void NudgeAim()
        {
            int strength = SettingsService.Instance != null ? SettingsService.Instance.AimAssist : 0;
            if (strength <= 0) return;
            Transform threat = NearestThreat();
            if (threat == null) return;
            Vector3 to = threat.position - transform.position;
            float yaw = PlayOptions.Yaw(transform.forward.x, transform.forward.z, to.x, to.z, strength, Time.deltaTime);
            if (yaw > 0.01f || yaw < -0.01f) transform.Rotate(0f, yaw, 0f, Space.World);
        }

        private Transform NearestThreat()
        {
            var hits = Physics.OverlapSphere(transform.position, 18f, GameLayers.EnemyMask);
            Transform best = null;
            float bestDist = 18f * 18f;
            for (int i = 0; i < hits.Length; i++)
            {
                var zombie = hits[i].GetComponentInParent<ZombieAI>();
                if (zombie == null || zombie.CurrentState == ZombieAI.ZombieState.Dead) continue;
                float dx = zombie.transform.position.x - transform.position.x;
                float dz = zombie.transform.position.z - transform.position.z;
                float dist = dx * dx + dz * dz;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = zombie.transform;
                }
            }
            return best;
        }

        private float StaminaPool()
        {
            float pool = NeedsPressure.Pool(SurvivorRoster.LeaderPractice("Guard"), maxStamina);
            var needs = GetComponent<SurvivalNeeds>();
            return needs != null ? needs.StaminaCap(pool) : pool;
        }

        private void HandleStamina()
        {
            var needs = GetComponent<SurvivalNeeds>();
            float cap = StaminaPool();
            if (currentStamina > cap) currentStamina = cap;
            if (IsSprinting)
            {
                currentStamina = Mathf.Max(0f, currentStamina - staminaDrainRate * Time.deltaTime);
                lastStaminaDrainTime = Time.time;
                OnStaminaChanged?.Invoke(currentStamina, cap);
            }
            else if (Time.time - lastStaminaDrainTime >= staminaRegenDelay)
            {
                if (currentStamina < cap)
                {
                    float rate = IsCrouching ? staminaRegenRate * 1.4f : staminaRegenRate;
                    if (needs != null) rate *= needs.StaminaRegenMultiplier;
                    currentStamina = Mathf.Min(cap, currentStamina + rate * Time.deltaTime);
                    OnStaminaChanged?.Invoke(currentStamina, cap);
                }
            }
        }

        private bool TakingDown()
        {
            var hands = GetComponent<PlayerInteractor>();
            return hands != null && hands.TakingDown;
        }

        private void HandleWeapons()
        {
            if (WheelOpen || Time.time < dodgeUntil || TakingDown() || ActiveWeapon == null) return;

            bool automatic = ActiveWeapon is FirearmWeapon gun && gun.Automatic;
            bool fire = ActiveWeapon is FirearmWeapon
                ? TriggerGate.ShouldFire(automatic, ExpeditionInput.FireHeld, ExpeditionInput.FirePressed)
                : ExpeditionInput.FireHeld;
            if (fire && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.CampManagement)
            {
                if (ActiveWeapon.TryAttack(transform.forward))
                {
                    CodexDirector.Hear("fire");
                    GetComponent<SurvivorLocomotion>()?.NotifyAttack();
                    GetComponent<ProceduralSurvivorMotion>()?.Strike();
                }
            }
        }

        public void SelectWeapon(int index)
        {
            if (equippedWeapons == null || equippedWeapons.Length == 0) return;
            if (index < 0 || index >= equippedWeapons.Length) return;

            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] != null)
                {
                    equippedWeapons[i].gameObject.SetActive(i == index);
                }
            }

            activeWeaponIndex = index;
            if (equippedWeapons[activeWeaponIndex] != null)
            {
                equippedWeapons[activeWeaponIndex].Initialize(transform);
            }

            OnActiveWeaponChanged?.Invoke(ActiveWeapon);
        }

        public void CycleWeapon(int direction)
        {
            if (equippedWeapons == null || equippedWeapons.Length <= 1) return;
            int nextIndex = (activeWeaponIndex + direction + equippedWeapons.Length) % equippedWeapons.Length;
            SelectWeapon(nextIndex);
        }

        private void HandleHurt(float amount, Vector3 point)
        {
            if (!Combat.ContactCue.Pain(true, healthSystem.CurrentHealth)) return;
            Shell.AudioManager.Instance?.Play("pained", 0.4f, 0.92f);
        }

        private void HandlePlayerDeath(Vector3 hitPoint, Vector3 hitDir, GameObject killer)
        {
            Debug.LogWarning("[PlayerController] Player died!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerPlayerDeath();
            }
        }
    }
}

using System;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Combat;
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

        // Components
        private CharacterController characterController;
        private HealthSystem healthSystem;
        private PlayerInventory inventory;
        private Camera mainCamera;

        // State Flags
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsAimingDownSights { get; private set; }
        public bool FlashlightOn => flashlightOn;
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

        public void Configure(WeaponBase[] weapons, Light tacticalLight)
        {
            equippedWeapons = weapons;
            flashlight = tacticalLight;
            groundAimMask = GameLayers.EnvironmentMask | 1;
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

            if (flashlight != null)
            {
                flashlight.enabled = flashlightOn;
            }
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
                flashlightOn = !flashlightOn;
                if (flashlight != null) flashlight.enabled = flashlightOn;
            }

            if (ExpeditionInput.MedkitPressed)
            {
                if (inventory != null && inventory.UseMedkit())
                {
                    GameplayFeedback.Toast("Medkit used  +50 HP");
                }
            }

            if (ExpeditionInput.ReloadPressed && ActiveWeapon is FirearmWeapon firearm)
            {
                firearm.TryStartReload();
            }

            IsAimingDownSights = ExpeditionInput.AimHeld;

            for (int i = 0; i < 4; i++)
            {
                if (ExpeditionInput.WeaponSlotPressed(i)) SelectWeapon(i);
            }

            float scroll = ExpeditionInput.Scroll;
            if (scroll > 0.05f) CycleWeapon(1);
            else if (scroll < -0.05f) CycleWeapon(-1);
            int padCycle = ExpeditionInput.WeaponCycle;
            if (padCycle != 0) CycleWeapon(padCycle);
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

            Vector2 move = ExpeditionInput.Move;
            Vector3 inputDirection = new Vector3(move.x, 0f, move.y);
            if (inputDirection.sqrMagnitude > 1f) inputDirection.Normalize();

            bool isMoving = inputDirection.sqrMagnitude > 0.01f;
            if (isMoving) TutorialDirector.Instance?.Note("move");

            IsCrouching = ExpeditionInput.CrouchHeld;
            if (IsCrouching) TutorialDirector.Instance?.Note("crouch");
            bool wantsToSprint = ExpeditionInput.SprintHeld && !IsCrouching && currentStamina > 5f;

            IsSprinting = isMoving && wantsToSprint;

            // Speed evaluation
            float currentSpeed = walkSpeed;
            if (IsCrouching) currentSpeed = crouchSpeed;
            else if (IsSprinting) currentSpeed = sprintSpeed;
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
            Vector2 aimStick = ExpeditionInput.AimStick;
            if (aimStick.sqrMagnitude > 0.04f)
            {
                Vector3 stickDir = new Vector3(aimStick.x, 0f, aimStick.y);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(stickDir), rotationSpeed * Time.deltaTime);
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(ExpeditionInput.Pointer);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDirection = (hitPoint - transform.position);
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private void HandleStamina()
        {
            if (IsSprinting)
            {
                currentStamina = Mathf.Max(0f, currentStamina - staminaDrainRate * Time.deltaTime);
                lastStaminaDrainTime = Time.time;
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
            else if (Time.time - lastStaminaDrainTime >= staminaRegenDelay)
            {
                if (currentStamina < maxStamina)
                {
                    float rate = IsCrouching ? staminaRegenRate * 1.4f : staminaRegenRate;
                var needs = GetComponent<SurvivalNeeds>();
                if (needs != null) rate *= needs.StaminaRegenMultiplier;
                    currentStamina = Mathf.Min(maxStamina, currentStamina + rate * Time.deltaTime);
                    OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                }
            }
        }

        private void HandleWeapons()
        {
            if (ActiveWeapon == null) return;

            // Attack (Left Mouse Button)
            if (ExpeditionInput.FireHeld && GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.CampManagement)
            {
                if (ActiveWeapon.TryAttack(transform.forward))
                {
                    TutorialDirector.Instance?.Note("fire");
                    GetComponent<SurvivorLocomotion>()?.NotifyAttack();
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

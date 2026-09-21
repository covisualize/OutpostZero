using System;
using System.Collections;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    public class FirearmWeapon : WeaponBase
    {
        [Header("Ammunition")]
        [SerializeField] private int maxMagazine = 12;
        [SerializeField] private int currentAmmo = 12;
        [SerializeField] private int reserveAmmo = 60;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private bool isReloading = false;

        [Header("Shooting Properties")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private float spreadAngle = 2.5f;
        [SerializeField] private int projectilesPerShot = 1; // 1 for pistol/rifle, 6-8 for shotgun

        [Header("Visual & Audio Feedback")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptyClickSound;

        public int CurrentAmmo => currentAmmo;
        public int MaxMagazine => maxMagazine;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;

        public event Action<int, int> OnAmmoChanged; // current, reserve
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;

        private float heat;

        private void Reset()
        {
            hitMask = GameLayers.WeaponHitMask;
        }

        private void OnValidate()
        {
            hitMask = GameLayers.Resolve(hitMask, GameLayers.WeaponHitMask);
        }

        private void Awake()
        {
            hitMask = GameLayers.Resolve(hitMask, GameLayers.WeaponHitMask);
        }

        public override void Configure(WeaponDefinition definition)
        {
            base.Configure(definition);
            if (definition == null) return;

            maxMagazine = Mathf.Max(1, definition.maxMagazine);
            currentAmmo = maxMagazine;
            reserveAmmo = Mathf.Max(0, definition.reserveAmmo);
            reloadDuration = definition.reloadDuration;
            spreadAngle = definition.spreadAngle;
            projectilesPerShot = Mathf.Max(1, definition.projectilesPerShot);
            hitMask = GameLayers.WeaponHitMask;
        }

        private void Start()
        {
            hitMask = GameLayers.Resolve(hitMask, GameLayers.WeaponHitMask);
            if (muzzlePoint == null)
            {
                muzzlePoint = transform;
            }
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }

        private void Update()
        {
            if (heat > 0f) heat = Mathf.Max(0f, heat - 28f * Time.deltaTime);
        }

        public override bool CanAttack()
        {
            return base.CanAttack() && !isReloading;
        }

        public override bool TryAttack(Vector3 targetDirection)
        {
            if (!CanAttack()) return false;

            if (currentAmmo <= 0)
            {
                PlaySound(emptyClickSound);
                nextAttackTime = Time.time + (1f / attackRate);
                TryStartReload();
                return false;
            }

            nextAttackTime = Time.time + (1f / attackRate);
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

            // Audio & Visual Effects
            PlaySound(fireSound);
            if (muzzleFlash != null) muzzleFlash.Play();

            // Emit gunshot noise event into the environment
            EmitWeaponNoise();

            // Fire projectiles
            heat = Mathf.Min(100f, heat + 7f);
            float spread = spreadAngle * SpreadMultiplier * (1f + heat / 80f);
            for (int i = 0; i < projectilesPerShot; i++)
            {
                Vector3 shootDir = ApplySpread(targetDirection, spread);
                FireSingleProjectile(shootDir);
            }

            TriggerAttackEvent();
            return true;
        }

        private void FireSingleProjectile(Vector3 direction)
        {
            Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position;

            if (bulletPrefab != null)
            {
                GameObject projObj = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(direction));
                var bullet = projObj.GetComponent<BulletProjectile>();
                if (bullet != null)
                {
                    bullet.Setup(direction, ModifiedDamage, ownerGameObject, hitMask);
                }
            }
            else if (Physics.Raycast(spawnPos, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                DamageResolver.Resolve(hit, ModifiedDamage, ownerGameObject, true);
            }
        }

        private Vector3 ApplySpread(Vector3 forward, float angle)
        {
            if (angle <= 0.01f) return forward;

            float randomYaw = UnityEngine.Random.Range(-angle, angle);
            Quaternion rot = Quaternion.AngleAxis(randomYaw, Vector3.up);
            return rot * forward;
        }

        public void TryStartReload()
        {
            if (isReloading || currentAmmo >= maxMagazine || reserveAmmo <= 0) return;

            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            OnReloadStarted?.Invoke();
            PlaySound(reloadSound);

            yield return new WaitForSeconds(reloadDuration);

            int needed = maxMagazine - currentAmmo;
            int loaded = Mathf.Min(needed, reserveAmmo);

            currentAmmo += loaded;
            reserveAmmo -= loaded;
            isReloading = false;

            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            OnReloadCompleted?.Invoke();
        }

        public void AddReserveAmmo(int amount)
        {
            reserveAmmo += amount;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}

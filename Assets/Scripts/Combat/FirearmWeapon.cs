using System;
using System.Collections;
using UnityEngine;
using OutpostZero.Colony;
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
        private bool abortReload;

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
        public int MaxMagazine => MagazineCapacity;

        private int brassRound;

        private int MagazineCapacity
        {
            get
            {
                var mod = GetComponent<WeaponMod>();
                return maxMagazine + (mod != null ? mod.magazineBonus : 0);
            }
        }
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;
        public float ReloadFill => isReloading ? MagPulse.Fill(reloadElapsed, reloadWait > 0f ? reloadWait : reloadDuration) : 0f;
        private float reloadElapsed;
        private float reloadWait;

        public event Action<int, int> OnAmmoChanged; // current, reserve
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;

        private float heat;
        private bool automatic;
        private bool useProjectile;
        private string cardId = "";

        public bool Automatic => automatic;
        public bool Projectile => useProjectile;
        public string CardId => string.IsNullOrEmpty(cardId) ? WeaponCard.IdFor(weaponType) : cardId;

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
            automatic = WeaponCard.FiresAutomatic(weaponType, definition.automatic);
            useProjectile = WeaponCard.FiresProjectile(weaponType, definition.useProjectile);
            if (!string.IsNullOrEmpty(definition.id)) cardId = definition.id;
        }

        public void SetFireMode(bool fullAuto, bool projectile)
        {
            automatic = fullAuto;
            useProjectile = projectile;
        }

        public void LoadCard(WeaponCard.Spec spec, int magazine, int spare)
        {
            cardId = spec.Id;
            weaponName = spec.Name;
            weaponType = spec.Type;
            baseDamage = spec.Damage;
            attackRate = Mathf.Max(0.1f, spec.Rate);
            range = spec.Range;
            spreadAngle = spec.Spread;
            projectilesPerShot = Mathf.Max(1, spec.Pellets);
            maxMagazine = Mathf.Max(1, spec.Magazine);
            reloadDuration = spec.Reload;
            noiseRadius = spec.Noise;
            noiseType = spec.NoiseKind;
            automatic = spec.Automatic;
            useProjectile = spec.Projectile;
            currentAmmo = Mathf.Clamp(magazine, 0, MagazineCapacity);
            reserveAmmo = Mathf.Max(0, spare);
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
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
            heat = RecoilBloom.Cool(heat, Time.deltaTime);
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
                Cue(GunCue.Click(currentAmmo, isReloading));
                nextAttackTime = Time.time + (1f / attackRate);
                TryStartReload();
                return false;
            }

            nextAttackTime = Time.time + (1f / attackRate);
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);

            // Audio & Visual Effects
            PlaySound(fireSound);
            bool quietFlash = Core.SettingsService.Instance != null && Core.SettingsService.Instance.QuietFlash;
            if (muzzleFlash != null && FlashCap.Allow(FlashCap.Stamp, Time.time, quietFlash)) muzzleFlash.Play();

            // Emit gunshot noise event into the environment
            EmitWeaponNoise();

            // Fire projectiles
            heat = RecoilBloom.AfterShot(heat);
            brassRound++;
            bool showTracer = BrassCue.Tracer(weaponType, brassRound);
            int guard = SurvivorRoster.LeaderPractice("Guard");
            float spread = RecoilBloom.Spread(spreadAngle, SpreadMultiplier * FieldHand.Spread(guard) * HandDepth.Spread(guard) * TraitHook.Aim(SurvivorRoster.LeaderTrait(), SurvivorRoster.LeaderAside(), SurvivorRoster.LeaderMark()), heat);
            var sights = ownerGameObject != null ? ownerGameObject.GetComponent<OutpostZero.Player.PlayerController>() : null;
            spread = SightGroup.Angle(spread, sights != null && sights.IsAimingDownSights);
            for (int i = 0; i < projectilesPerShot; i++)
            {
                Vector3 shootDir = ApplySpread(targetDirection, spread);
                FireSingleProjectile(shootDir, showTracer);
            }

            TriggerAttackEvent();
            return true;
        }

        private void FireSingleProjectile(Vector3 direction, bool tracer)
        {
            Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position;

            if (useProjectile || bulletPrefab != null)
            {
                GameObject projObj = bulletPrefab != null
                    ? Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(direction))
                    : CreateBullet(spawnPos, direction);
                var bullet = projObj.GetComponent<BulletProjectile>() ?? projObj.AddComponent<BulletProjectile>();
                bullet.Setup(direction, ModifiedDamage, ownerGameObject, hitMask, weaponType, range);
                Vector3 eject = muzzlePoint != null ? muzzlePoint.right : transform.right;
                CombatVfx.Shot(spawnPos, direction, spawnPos + direction * Mathf.Min(range, 8f), eject, tracer, weaponType);
            }
            else
            {
                Vector3 end = spawnPos + direction * range;
                if (Physics.Raycast(spawnPos, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    CombatEvents.NoteDir(direction);
                    float amount = PelletDrop.Damage(ModifiedDamage, hit.distance, range, weaponType);
                    DamageResolver.Resolve(hit, amount, ownerGameObject, true, weaponType);
                }
                Vector3 eject = muzzlePoint != null ? muzzlePoint.right : transform.right;
                CombatVfx.Shot(spawnPos, direction, end, eject, tracer, weaponType);
            }
        }

        private static GameObject CreateBullet(Vector3 spawnPos, Vector3 direction)
        {
            var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "Bullet";
            bullet.transform.position = spawnPos;
            bullet.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction : Vector3.forward);
            bullet.transform.localScale = Vector3.one * 0.06f;
            bullet.layer = GameLayers.Projectile;
            var collider = bullet.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = bullet.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            bullet.AddComponent<BulletProjectile>();
            return bullet;
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
            if (isReloading || currentAmmo >= MagazineCapacity || reserveAmmo <= 0) return;

            StartCoroutine(ReloadRoutine());
        }

        public bool TryAbortReload(bool sprinting, bool hit)
        {
            if (!isReloading) return false;
            float fill = reloadWait > 0.001f ? reloadElapsed / reloadWait : 0f;
            if (!ReloadBreak.Abort(sprinting, hit, fill)) return false;
            abortReload = true;
            return true;
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            reloadElapsed = 0f;
            int guardSkill = SurvivorRoster.LeaderPractice("Guard");
            reloadWait = reloadDuration * FieldHand.Reload(guardSkill) * HandDepth.Reload(guardSkill);
            OnReloadStarted?.Invoke();
            PlaySound(reloadSound);
            int stage = 0;
            string open = GunCue.Stage(0f, stage);
            stage = GunCue.Mark(open);
            Cue(open);

            while (reloadElapsed < reloadWait)
            {
                if (abortReload)
                {
                    abortReload = false;
                    isReloading = false;
                    reloadElapsed = 0f;
                    Cue("clack");
                    yield break;
                }
                reloadElapsed += Time.deltaTime;
                string beat = GunCue.Stage(MagPulse.Fill(reloadElapsed, reloadWait), stage);
                if (beat.Length > 0)
                {
                    stage = GunCue.Mark(beat);
                    Cue(beat);
                }
                yield return null;
            }
            reloadElapsed = reloadWait;

            int needed = MagazineCapacity - currentAmmo;
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

        public bool TrySpendRound()
        {
            if (reserveAmmo > 0) reserveAmmo--;
            else if (currentAmmo > 0) currentAmmo--;
            else return false;
            OnAmmoChanged?.Invoke(currentAmmo, reserveAmmo);
            return true;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void Cue(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var ear = OutpostZero.Shell.AudioManager.Instance;
            if (ear == null) return;
            Vector3 at = muzzlePoint != null ? muzzlePoint.position : transform.position;
            ear.PlayAt(id, at, id == "dry" ? 0.32f : 0.4f);
        }
    }
}

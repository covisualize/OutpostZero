using System.Collections.Generic;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Camera trauma, hit flash, short hit-stop, knockback into NavMesh agents, and floating damage numbers.
    /// </summary>
    public class HitFeedback : MonoBehaviour
    {
        public static HitFeedback Instance { get; private set; }

        [SerializeField] private bool hitStopOnMeleeKill = true;
        private readonly List<FloatingDamage> numbers = new List<FloatingDamage>();
        private float hitStopUntil;
        private float previousTimeScale = 1f;

        public struct FloatingDamage
        {
            public Vector3 World;
            public string Text;
            public float Until;
            public bool Crit;
        }

        public IReadOnlyList<FloatingDamage> Popups => numbers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            CombatEvents.OnHit += HandleHit;
            CombatEvents.OnKill += HandleKill;
            CombatEvents.OnShotFired += HandleShot;
        }

        private void OnDisable()
        {
            CombatEvents.OnHit -= HandleHit;
            CombatEvents.OnKill -= HandleKill;
            CombatEvents.OnShotFired -= HandleShot;
        }

        private void Update()
        {
            if (hitStopUntil > 0f && Time.unscaledTime >= hitStopUntil && Time.timeScale < 0.2f)
            {
                Time.timeScale = previousTimeScale;
                hitStopUntil = 0f;
            }
        }

        public void AddNumber(Vector3 world, float amount, bool crit)
        {
            if (SettingsService.Instance != null && !SettingsService.Instance.DamageNumbers) return;
            numbers.Add(new FloatingDamage
            {
                World = world + Vector3.up * 1.6f,
                Text = Mathf.CeilToInt(amount).ToString(),
                Until = Time.time + 0.7f,
                Crit = crit
            });
        }

        private void HandleShot(Vector3 muzzle, WeaponBase weapon)
        {
            if (weapon == null) return;
            OutpostZero.Graphics.ExpeditionCameraRig.At(OutpostZero.Graphics.CameraTuning.ShotShake(weapon.Type), muzzle);
        }

        private void HandleHit(Vector3 point, Vector3 normal, GameObject target)
        {
            if (target == null) return;
            Flash(target);
            var zombie = target.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                float stun = CombatEvents.FromWeapon ? HitStun.Seconds(CombatEvents.LastWeapon) : 0.25f;
                zombie.ApplyImpulse(normal.sqrMagnitude > 0.01f ? -normal : target.transform.forward, 1.4f, stun);
            }
        }

        private void HandleKill(GameObject victim, GameObject killer)
        {
            if (!hitStopOnMeleeKill || killer == null) return;
            if (SettingsService.Instance != null && !SettingsService.Instance.HitStop) return;
            var melee = killer.GetComponentInChildren<MeleeWeapon>();
            if (melee == null || !melee.isActiveAndEnabled) return;
            if (Time.timeScale < 0.2f) return;
            previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0.05f;
            hitStopUntil = Time.unscaledTime + 0.03f;
        }

        private static void Flash(GameObject target)
        {
            OutpostZero.Graphics.HitGlow.Strike(target);
        }

        public void PrunePopups()
        {
            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                if (Time.time > numbers[i].Until) numbers.RemoveAt(i);
            }
        }
    }
}

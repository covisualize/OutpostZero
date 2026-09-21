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

        private struct FloatingDamage
        {
            public Vector3 World;
            public string Text;
            public float Until;
            public bool Crit;
        }

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
            var camera = Camera.main != null ? Camera.main.GetComponent<TopDownCameraFollow>() : null;
            if (camera == null || weapon == null) return;
            float trauma = weapon.Type == Core.WeaponType.Shotgun ? 0.45f : weapon.Type == Core.WeaponType.Melee ? 0.1f : 0.15f;
            camera.AddTrauma(trauma * SettingsService.ShakeScale);
        }

        private void HandleHit(Vector3 point, Vector3 normal, GameObject target)
        {
            if (target == null) return;
            Flash(target);
            var zombie = target.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                zombie.ApplyImpulse(normal.sqrMagnitude > 0.01f ? -normal : target.transform.forward, 1.4f, 0.25f);
            }
        }

        private void HandleKill(GameObject victim, GameObject killer)
        {
            if (!hitStopOnMeleeKill || killer == null) return;
            var melee = killer.GetComponentInChildren<MeleeWeapon>();
            if (melee == null || !melee.isActiveAndEnabled) return;
            if (Time.timeScale < 0.2f) return;
            previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0.05f;
            hitStopUntil = Time.unscaledTime + 0.03f;
        }

        private static void Flash(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.white);
                renderer.SetPropertyBlock(block);
            }
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;
            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                if (Time.time > numbers[i].Until)
                {
                    numbers.RemoveAt(i);
                    continue;
                }
                Vector3 screen = cam.WorldToScreenPoint(numbers[i].World);
                if (screen.z < 0f) continue;
                var style = new GUIStyle(GUI.skin.label) { fontSize = numbers[i].Crit ? 18 : 14, fontStyle = FontStyle.Bold };
                style.normal.textColor = numbers[i].Crit ? new Color(1f, 0.85f, 0.2f) : Color.white;
                GUI.Label(new Rect(screen.x, Screen.height - screen.y, 80f, 24f), numbers[i].Text, style);
            }
        }
    }
}

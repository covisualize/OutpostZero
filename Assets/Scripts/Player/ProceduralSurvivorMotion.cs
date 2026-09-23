using UnityEngine;
using OutpostZero.Combat;

namespace OutpostZero.Player
{
    /// <summary>
    /// Crouch scale and step bob for the leader mesh. The exported characters are still static meshes,
    /// so locomotion reads as motion until humanoid clips exist.
    /// A shot, a reload, a hit, and a death pose the same child when no rig is driving it.
    /// </summary>
    public class ProceduralSurvivorMotion : MonoBehaviour
    {
        private PlayerController controller;
        private CharacterController body;
        private Transform visual;
        private Quaternion rest = Quaternion.identity;
        private bool captured;
        private float bob;
        private float attackAge = 10f;
        private float hitAge = 10f;
        private float reloadAge;
        private float deathAge;
        private bool attackLive;
        private bool hitLive;
        private bool reloadLive;
        private bool dead;
        private HealthSystem health;
        private FirearmWeapon[] guns;

        public void Strike()
        {
            if (dead) return;
            attackLive = true;
            attackAge = 0f;
        }

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            body = GetComponent<CharacterController>();
        }

        private void Start()
        {
            health = GetComponent<HealthSystem>();
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnDeath += HandleDeath;
            }
            guns = GetComponentsInChildren<FirearmWeapon>(true);
            for (int i = 0; i < guns.Length; i++)
            {
                if (guns[i] == null) continue;
                guns[i].OnReloadStarted += HandleReload;
                guns[i].OnReloadCompleted += HandleReloadDone;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamaged -= HandleDamaged;
                health.OnDeath -= HandleDeath;
            }
            if (guns == null) return;
            for (int i = 0; i < guns.Length; i++)
            {
                if (guns[i] == null) continue;
                guns[i].OnReloadStarted -= HandleReload;
                guns[i].OnReloadCompleted -= HandleReloadDone;
            }
        }

        private void HandleDamaged(float amount, Vector3 point)
        {
            if (dead || amount <= 0f) return;
            hitLive = true;
            hitAge = 0f;
        }

        private void HandleDeath(Vector3 point, Vector3 direction, GameObject attacker)
        {
            dead = true;
            deathAge = 0f;
        }

        private void HandleReload()
        {
            if (dead) return;
            reloadLive = true;
            reloadAge = 0f;
        }

        private void HandleReloadDone()
        {
            reloadLive = false;
        }

        private void LateUpdate()
        {
            if (visual == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Survivor") || child.name.Contains("Mesh") || child.name.Contains("Visual"))
                    {
                        visual = child;
                        break;
                    }
                }
                if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            }
            if (visual == null || controller == null) return;
            if (!captured)
            {
                rest = visual.localRotation;
                captured = true;
            }

            float speed = body != null ? body.velocity.magnitude : 0f;
            bob += speed * Time.deltaTime * 2.2f;
            float step = Time.deltaTime;
            if (dead) deathAge += step;
            if (attackLive)
            {
                attackAge += step;
                if (attackAge >= GaitSheet.SwingTime) attackLive = false;
            }
            if (hitLive)
            {
                hitAge += step;
                if (hitAge >= GaitSheet.HitTime) hitLive = false;
            }
            if (reloadLive) reloadAge += step;

            bool hitting = hitLive && GaitSheet.Flail(hitAge) > 0f;
            bool attacking = attackLive && GaitSheet.Swing(attackAge) > 0f;
            var beat = GaitSheet.Pick(dead, hitting, attacking, reloadLive, controller.IsCrouching, controller.IsSprinting, speed, controller.IsAimingDownSights);
            float age = beat == GaitSheet.Beat.Dead ? deathAge
                : beat == GaitSheet.Beat.Hit ? hitAge
                : beat == GaitSheet.Beat.Attack ? attackAge
                : beat == GaitSheet.Beat.Reload ? reloadAge
                : 0f;
            float lean = GaitSheet.Lean(beat, age);
            float hop = GaitSheet.Hop(beat, bob);
            float sink = GaitSheet.Sink(beat, age);
            float scale = GaitSheet.Scale(controller.IsCrouching, dead);
            visual.localScale = new Vector3(1f, scale, 1f);
            visual.localPosition = new Vector3(0f, hop + sink, 0f);
            visual.localRotation = rest * Quaternion.Euler(lean, 0f, 0f);
        }
    }
}

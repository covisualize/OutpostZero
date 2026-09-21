using UnityEngine;
using OutpostZero.Combat;

namespace OutpostZero.Player
{
    /// <summary>
    /// Idle, walk, crouch, and sprint. A rigged body uses the SurvivorLocomotion controller.
    /// A mesh with no avatar falls back to a bob on the visual child, not the character root.
    /// </summary>
    public class SurvivorLocomotion : MonoBehaviour
    {
        public enum Pose
        {
            Idle,
            Walk,
            Crouch,
            Sprint
        }

        private Animation animationPlayer;
        private Animator animator;
        private PlayerController controller;
        private Transform visual;
        private Pose pose = Pose.Idle;
        private HealthSystem health;
        private FirearmWeapon[] guns;

        public Pose CurrentPose => pose;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                var controllerAsset = Resources.Load<RuntimeAnimatorController>("SurvivorLocomotion");
                if (controllerAsset != null) animator.runtimeAnimatorController = controllerAsset;
            }
            if (animator != null && animator.runtimeAnimatorController != null) return;
            visual = FindVisual();
            if (visual == null) return;
            animationPlayer = visual.gameObject.GetComponent<Animation>() ?? visual.gameObject.AddComponent<Animation>();
            animationPlayer.playAutomatically = false;
            AddClip(Pose.Idle, 0.02f, 1.4f);
            AddClip(Pose.Walk, 0.06f, 0.45f);
            AddClip(Pose.Crouch, 0.03f, 0.7f);
            AddClip(Pose.Sprint, 0.1f, 0.28f);
            animationPlayer.Play(Pose.Idle.ToString());
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
                if (guns[i] != null) guns[i].OnReloadStarted += HandleReload;
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
                if (guns[i] != null) guns[i].OnReloadStarted -= HandleReload;
            }
        }

        public void NotifyAttack()
        {
            if (animator != null) animator.SetTrigger("Attack");
        }

        private void HandleReload()
        {
            if (animator != null) animator.SetTrigger("Reload");
        }

        private void HandleDamaged(float amount, Vector3 point)
        {
            if (animator != null && amount > 0f) animator.SetTrigger("Hit");
        }

        private void HandleDeath(Vector3 point, Vector3 direction, GameObject attacker)
        {
            if (animator != null) animator.SetTrigger("Death");
        }

        private Transform FindVisual()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.transform == transform) continue;
                string name = renderer.gameObject.name;
                if (name.Contains("Weapon") || name.Contains("Rifle") || name.Contains("Pistol") || name.Contains("Shotgun") || name.Contains("Machete"))
                {
                    continue;
                }
                return renderer.transform;
            }
            return null;
        }

        private void AddClip(Pose id, float amplitude, float duration)
        {
            var clip = new AnimationClip { legacy = true };
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.5f, amplitude),
                new Keyframe(duration, 0f));
            clip.SetCurve("", typeof(Transform), "localPosition.y", curve);
            clip.wrapMode = WrapMode.Loop;
            animationPlayer.AddClip(clip, id.ToString());
        }

        private void Update()
        {
            if (controller == null) return;
            Pose next = Pose.Idle;
            var body = GetComponent<CharacterController>();
            float speed = body != null ? body.velocity.magnitude : 0f;
            if (controller.IsCrouching && speed > 0.2f) next = Pose.Crouch;
            else if (controller.IsSprinting) next = Pose.Sprint;
            else if (speed > 0.2f) next = Pose.Walk;
            if (animator != null)
            {
                animator.SetFloat("Speed", speed);
                animator.SetBool("Crouch", controller.IsCrouching);
                animator.SetBool("Sprint", controller.IsSprinting);
            }
            if (next == pose || animationPlayer == null) 
            {
                pose = next;
                return;
            }
            pose = next;
            animationPlayer.CrossFade(pose.ToString(), 0.12f);
        }
    }
}

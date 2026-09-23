using UnityEngine;
using OutpostZero.Core;
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
        private float aimWeight;
        private Transform socket;
        private Vector3 socketRest;
        private Transform hand;
        private Vector3 handRest;
        private bool handSettled;
        private float reloadClip;

        public Pose CurrentPose => pose;
        public float AimWeight => aimWeight;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            animator = Attach.Near<Animator>(this);
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                var controllerAsset = Resources.Load<RuntimeAnimatorController>("SurvivorLocomotion");
                if (controllerAsset != null) animator.runtimeAnimatorController = controllerAsset;
            }
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                Arm(animator.gameObject);
                AddClipEvents(animator.runtimeAnimatorController);
                return;
            }
            visual = FindVisual();
            if (visual == null) return;
            animationPlayer = Attach.Ensure<Animation>(visual.gameObject);
            animationPlayer.playAutomatically = false;
            AddClip(Pose.Idle, 0.02f, 1.4f);
            AddClip(Pose.Walk, 0.06f, 0.45f);
            AddClip(Pose.Crouch, 0.03f, 0.7f);
            AddClip(Pose.Sprint, 0.1f, 0.28f);
            animationPlayer.Play(Pose.Idle.ToString());
            Arm(visual.gameObject);
        }

        private void Arm(GameObject host)
        {
            if (host == null || host == gameObject) return;
            if (host.GetComponent<FootstepRelay>() == null) host.AddComponent<FootstepRelay>();
        }

        public void OnFootstep()
        {
            controller?.PlayFootstep();
            Shell.AudioManager.Instance?.Footfall();
        }

        /// <summary>Adds footfalls and the reload finish to the imported clips, once per shared clip.</summary>
        private void AddClipEvents(RuntimeAnimatorController rig)
        {
            foreach (var clip in rig.animationClips)
            {
                if (clip == null) continue;
                string name = clip.name;
                int bar = name.LastIndexOf('|');
                if (bar >= 0) name = name.Substring(bar + 1);
                if (name == "Reload") reloadClip = clip.length;
                float[] marks = AimRig.EventsFor(name, out string function);
                if (marks.Length == 0 || Carries(clip, function)) continue;
                for (int i = 0; i < marks.Length; i++)
                    clip.AddEvent(new AnimationEvent { time = clip.length * marks[i], functionName = function });
            }
        }

        private static bool Carries(AnimationClip clip, string function)
        {
            var events = clip.events;
            for (int i = 0; i < events.Length; i++)
                if (events[i].functionName == function) return true;
            return false;
        }

        public void OnReloadAnimComplete()
        {
            var gun = controller != null ? controller.ActiveWeapon as FirearmWeapon : null;
            if (gun != null) gun.FinishFromAnimation();
        }

        /// <summary>The humanoid look-at pass: chest and head turn toward the aim point inside the twist limit.</summary>
        public void ApplyAim(Animator rig)
        {
            if (rig == null || controller == null) return;
            bool alive = health == null || !health.IsDead;
            var gun = controller.ActiveWeapon as FirearmWeapon;
            float target = AimRig.Weight(alive, controller.IsSprinting, gun != null && gun.IsReloading, controller.IsAimingDownSights);
            aimWeight = AimRig.Blend(aimWeight, target, Time.deltaTime);
            Vector3 chest = transform.position + Vector3.up * 1.4f;
            Vector3 aim = controller.AimPoint.sqrMagnitude > 0f ? controller.AimPoint : transform.position + transform.forward * 6f;
            rig.SetLookAtWeight(aimWeight, AimRig.BodyWeight, AimRig.HeadWeight, 0f, AimRig.Clamp);
            rig.SetLookAtPosition(AimRig.Target(chest, transform.forward, aim));
        }
        private void Start()
        {
            health = GetComponent<HealthSystem>();
            if (health != null)
            {
                health.OnDamaged += HandleDamaged;
                health.OnDeath += HandleDeath;
            }
            socket = transform.Find("Weapon_Socket");
            if (socket != null) socketRest = socket.localPosition;
            if (animator != null && animator.isHuman) hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
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
            if (id != Pose.Idle)
            {
                clip.AddEvent(new AnimationEvent
                {
                    time = duration * 0.5f,
                    functionName = "OnFootstep"
                });
            }
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
                var gun = controller.ActiveWeapon as FirearmWeapon;
                animator.SetFloat("ReloadSpeed", gun != null && gun.IsReloading ? AimRig.ReloadSpeed(reloadClip, gun.ReloadSeconds) : 1f);
            }
            if (next == pose || animationPlayer == null) 
            {
                pose = next;
                return;
            }
            pose = next;
            animationPlayer.CrossFade(pose.ToString(), 0.12f);
        }

        private void LateUpdate()
        {
            if (socket == null || hand == null) return;
            Vector3 local = transform.InverseTransformPoint(hand.position);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Idle") && state.normalizedTime > 0.2f)
            {
                handRest = handSettled ? Vector3.Lerp(handRest, local, 0.05f) : local;
                handSettled = true;
            }
            if (!handSettled) return;
            socket.localPosition = AimRig.Socket(socketRest, handRest, local, AimRig.HandFollow);
        }
    }

    /// <summary>
    /// Animation events fire on the object that plays the clip. This forwards them to the body.
    /// </summary>
    public class FootstepRelay : MonoBehaviour
    {
        public void OnFootstep()
        {
            var body = GetComponentInParent<SurvivorLocomotion>();
            if (body != null) body.OnFootstep();
        }

        public void OnReloadAnimComplete()
        {
            var body = GetComponentInParent<SurvivorLocomotion>();
            if (body != null) body.OnReloadAnimComplete();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != 0) return;
            var rig = GetComponent<Animator>();
            var body = GetComponentInParent<SurvivorLocomotion>();
            if (body != null) body.ApplyAim(rig);
        }
    }
}

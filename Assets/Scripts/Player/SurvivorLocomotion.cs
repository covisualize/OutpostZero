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
        private Transform spine;
        private Transform head;
        private readonly BoneTurn spineTurn = new BoneTurn();
        private readonly BoneTurn headTurn = new BoneTurn();
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

        private void AddClipEvents(RuntimeAnimatorController rig)
        {
            foreach (var clip in rig.animationClips)
                if (clip != null && ClipEvents.Bare(clip.name) == "Reload") reloadClip = clip.length;
            ClipEvents.Arm(rig);
        }

        public void OnReloadAnimComplete()
        {
            var gun = controller != null ? controller.ActiveWeapon as FirearmWeapon : null;
            if (gun != null) gun.FinishFromAnimation();
        }

        private float AimTarget()
        {
            bool alive = health == null || !health.IsDead;
            var gun = controller.ActiveWeapon as FirearmWeapon;
            return AimRig.Weight(alive, controller.IsSprinting, gun != null && gun.IsReloading, controller.IsAimingDownSights);
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
            if (animator != null)
            {
                hand = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : Bone(animator.transform, AimRig.HandBone);
                spine = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Spine) : Bone(animator.transform, AimRig.SpineBone);
                head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : Bone(animator.transform, AimRig.HeadBone);
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

        private static Transform Bone(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = Bone(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void LateUpdate()
        {
            Turn();
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

        /// <summary>Turns the posed spine and head toward the aim point, after the animator has written the pose.</summary>
        private void Turn()
        {
            if (controller == null || spine == null) return;
            aimWeight = AimRig.Blend(aimWeight, AimTarget(), Time.deltaTime);
            Vector3 chest = transform.position + Vector3.up * 1.4f;
            Vector3 aim = controller.AimPoint.sqrMagnitude > 0f ? controller.AimPoint : transform.position + transform.forward * 6f;
            float yaw = AimRig.Yaw(transform.forward, chest, aim, aimWeight);
            spineTurn.Apply(spine, yaw * AimRig.SpineShare);
            if (head != null) headTurn.Apply(head, yaw * (1f - AimRig.SpineShare));
        }
    }

    /// <summary>
    /// Adds a turn about world up to a bone each frame without letting it build up when the animator
    /// leaves that bone alone: an untouched bone is put back to its pose before the new turn.
    /// </summary>
    public class BoneTurn
    {
        private Quaternion posed;
        private Quaternion written;
        private bool has;

        public void Apply(Transform bone, float degrees)
        {
            if (bone == null) return;
            if (has && Quaternion.Angle(bone.localRotation, written) < 0.01f) bone.localRotation = posed;
            posed = bone.localRotation;
            bone.rotation = Quaternion.AngleAxis(degrees, Vector3.up) * bone.rotation;
            written = bone.localRotation;
            has = true;
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

    }
}

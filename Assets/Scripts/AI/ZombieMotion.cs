using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.AI
{
    /// <summary>
    /// Drives the imported clips from the AI state through the model's own controller.
    /// A mesh with no animator or controller still leans and bobs.
    /// </summary>
    public class ZombieMotion : MonoBehaviour
    {
        private ZombieAI brain;
        private Transform visual;
        private Animator animator;
        private bool rigged;
        private int swings;
        private int variant;
        private int phase;
        private float pace;
        private Vector3 lastPosition;
        private readonly System.Collections.Generic.HashSet<int> parameters = new System.Collections.Generic.HashSet<int>();
        private ZombieAI.ZombieState driven = (ZombieAI.ZombieState)(-1);
        private float posed;
        private float bob;

        private void Awake()
        {
            brain = GetComponent<ZombieAI>();
        }

        private void LateUpdate()
        {
            if (brain == null) return;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && !rigged) Rig();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                DriveRig();
                return;
            }
            if (visual == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Mesh") || child.name.Contains("Zombie"))
                    {
                        visual = child;
                        break;
                    }
                }
                if (visual == null)
                {
                    var renderer = GetComponentInChildren<Renderer>();
                    if (renderer != null && renderer.transform != transform) visual = renderer.transform;
                }
            }
            if (visual == null) return;

            var state = brain.CurrentState;
            if (state != driven)
            {
                driven = state;
                posed = Time.time;
            }
            float age = Time.time - posed;
            bob += (PoseSheet.Moves(state) ? 6f : 1f) * Time.deltaTime;
            float lean = PoseSheet.Lean(state, age);
            float hop = PoseSheet.Hop(state, bob) + PoseSheet.Sink(state, age);
            visual.localRotation = Quaternion.Euler(lean, 0f, 0f);
            visual.localPosition = new Vector3(0f, hop, 0f);
        }

        /// <summary>Loads the model's controller once and arms its clips; the bite then waits for the contact frame.</summary>
        private void Rig()
        {
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            if (animator.runtimeAnimatorController == null)
            {
                string resource = CharacterRig.ResourcePath(brain.ModelId);
                if (string.IsNullOrEmpty(resource)) return;
                animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(resource);
            }
            rigged = true;
            if (animator.runtimeAnimatorController == null) return;
            if (animator.GetComponent<ZombieClipRelay>() == null)
                animator.gameObject.AddComponent<ZombieClipRelay>();
            brain.ClipDriven = ClipEvents.Arm(animator.runtimeAnimatorController, AimRig.AttackImpact);
            parameters.Clear();
            foreach (var parameter in animator.parameters) parameters.Add(parameter.nameHash);
            Restart();
        }

        /// <summary>Fresh pose for a spawn or a pooled body coming back.</summary>
        public void Restart()
        {
            if (brain == null) brain = GetComponent<ZombieAI>();
            if (brain == null) return;
            swings = brain.Swings;
            phase = 0;
            pace = 0f;
            driven = (ZombieAI.ZombieState)(-1);
            lastPosition = transform.position;
            int seed = gameObject.GetInstanceID();
            variant = CrowdVariant.Pick(seed);
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.Rebind();
            SetFloat(CharacterRig.IdleVariant, CrowdVariant.Idle(variant));
            SetFloat(CharacterRig.GaitVariant, CrowdVariant.Walk(variant));
            SetFloat(CharacterRig.DeathVariant, CrowdVariant.Death(seed));
            animator.Play("Idle", 0, CrowdVariant.CycleOffset(seed));
        }

        private void SetFloat(string name, float value)
        {
            if (parameters.Contains(Animator.StringToHash(name))) animator.SetFloat(name, value);
        }

        private void SetBool(string name, bool value)
        {
            if (parameters.Contains(Animator.StringToHash(name))) animator.SetBool(name, value);
        }

        private string Reel()
        {
            bool reels = HitStun.Staggers(brain.StunSeconds) && animator.HasState(0, Animator.StringToHash(CharacterRig.Stagger));
            return reels ? CharacterRig.Stagger : "Hit";
        }

        private void SetTrigger(string name)
        {
            if (parameters.Contains(Animator.StringToHash(name))) animator.SetTrigger(name);
        }

        private void DriveRig()
        {
            var state = brain.CurrentState;
            float dt = Time.deltaTime;
            Vector3 moved = transform.position - lastPosition;
            lastPosition = transform.position;
            moved.y = 0f;
            float measured = dt > 0.0001f ? moved.magnitude / dt : 0f;
            if (measured > 20f) measured = 0f;
            pace = Mathf.Lerp(pace, measured, 1f - Mathf.Exp(-10f * dt));
            if (state == ZombieAI.ZombieState.Dead) pace = 0f;

            string model = brain.ModelId;
            float scale = transform.lossyScale.y;
            float walkGround = StrideSheet.GroundSpeed(model, CrowdVariant.WalkClip(variant));
            float runGround = StrideSheet.GroundSpeed(model, "Sprint");
            bool dashing = brain.AbilityPhase == 2;
            bool running = state == ZombieAI.ZombieState.Chase && StrideSheet.Sprints(pace, walkGround, runGround, scale);
            float ground = dashing ? StrideSheet.GroundSpeed(model, "Charge") : running ? runGround : walkGround;

            animator.SetFloat("Speed", StrideSheet.SpeedParam(pace, brain.ChaseSpeed));
            animator.SetBool("Sprint", running);
            animator.SetBool("Crouch", false);
            SetFloat(StrideSheet.MoveRate, StrideSheet.Rate(pace, ground, scale));

            int nextPhase = brain.AbilityPhase;
            if (nextPhase == 1 && phase != 1) SetTrigger(CharacterRig.Windup);
            SetBool(CharacterRig.Dash, nextPhase == 2);
            phase = nextPhase;

            if (brain.Swings != swings)
            {
                swings = brain.Swings;
                animator.SetTrigger("Attack");
            }
            if (state == driven) return;
            if (state == ZombieAI.ZombieState.Stunned) animator.SetTrigger(Reel());
            else if (state == ZombieAI.ZombieState.Dead) animator.SetTrigger("Death");
            driven = state;
        }
    }

    /// <summary>Receives clip events on the mesh child and forwards them to the zombie.</summary>
    public class ZombieClipRelay : MonoBehaviour
    {
        public void OnAttackImpact()
        {
            var brain = GetComponentInParent<ZombieAI>();
            if (brain != null) brain.OnAttackImpact();
        }

        public void OnFootstep()
        {
        }
    }
}

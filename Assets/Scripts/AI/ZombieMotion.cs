using UnityEngine;
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
            swings = brain.Swings;
        }

        private void DriveRig()
        {
            var state = brain.CurrentState;
            animator.SetFloat("Speed", PoseSheet.Speed(state));
            animator.SetBool("Sprint", PoseSheet.Sprint(state));
            animator.SetBool("Crouch", false);
            if (brain.Swings != swings)
            {
                swings = brain.Swings;
                animator.SetTrigger("Attack");
            }
            if (state == driven) return;
            if (state == ZombieAI.ZombieState.Stunned) animator.SetTrigger("Hit");
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

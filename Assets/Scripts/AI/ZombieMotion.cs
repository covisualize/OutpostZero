using UnityEngine;

namespace OutpostZero.AI
{
    /// <summary>
    /// Drives the imported humanoid clips from the AI state.
    /// A mesh with no avatar still leans and bobs.
    /// </summary>
    public class ZombieMotion : MonoBehaviour
    {
        private ZombieAI brain;
        private Transform visual;
        private Animator animator;
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
            if (animator != null) animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
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

        private void DriveRig()
        {
            var state = brain.CurrentState;
            animator.SetFloat("Speed", PoseSheet.Speed(state));
            animator.SetBool("Sprint", PoseSheet.Sprint(state));
            animator.SetBool("Crouch", false);
            if (state == driven) return;
            if (state == ZombieAI.ZombieState.Attack) animator.SetTrigger("Attack");
            else if (state == ZombieAI.ZombieState.Stunned) animator.SetTrigger("Hit");
            else if (state == ZombieAI.ZombieState.Dead) animator.SetTrigger("Death");
            driven = state;
        }
    }
}

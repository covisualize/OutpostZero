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
        private float bob;

        private void Awake()
        {
            brain = GetComponent<ZombieAI>();
        }

        private void LateUpdate()
        {
            if (brain == null) return;
            if (animator == null) animator = GetComponentInChildren<Animator>();
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
            }
            if (visual == null) return;

            bool moving = brain.CurrentState == ZombieAI.ZombieState.Chase
                || brain.CurrentState == ZombieAI.ZombieState.Wander
                || brain.CurrentState == ZombieAI.ZombieState.Searching;
            bob += (moving ? 6f : 1f) * Time.deltaTime;
            float lean = brain.CurrentState == ZombieAI.ZombieState.Chase ? 14f : 0f;
            float hop = moving ? Mathf.Sin(bob) * 0.05f : 0f;
            visual.localRotation = Quaternion.Euler(lean, 0f, 0f);
            visual.localPosition = new Vector3(0f, hop, 0f);
        }

        private void DriveRig()
        {
            var state = brain.CurrentState;
            float speed = 0f;
            bool sprint = false;
            if (state == ZombieAI.ZombieState.Chase)
            {
                speed = 4.2f;
                sprint = true;
            }
            else if (state == ZombieAI.ZombieState.Wander || state == ZombieAI.ZombieState.Searching || state == ZombieAI.ZombieState.InvestigateNoise)
            {
                speed = 1.1f;
            }
            animator.SetFloat("Speed", speed);
            animator.SetBool("Sprint", sprint);
            animator.SetBool("Crouch", false);
            if (state == driven) return;
            if (state == ZombieAI.ZombieState.Attack) animator.SetTrigger("Attack");
            else if (state == ZombieAI.ZombieState.Stunned) animator.SetTrigger("Hit");
            else if (state == ZombieAI.ZombieState.Dead) animator.SetTrigger("Death");
            driven = state;
        }
    }
}

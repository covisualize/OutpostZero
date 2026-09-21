using UnityEngine;

namespace OutpostZero.AI
{
    /// <summary>
    /// Leans and bobs the zombie mesh from the AI state. There is no humanoid clip yet.
    /// </summary>
    public class ZombieMotion : MonoBehaviour
    {
        private ZombieAI brain;
        private Transform visual;
        private float bob;

        private void Awake()
        {
            brain = GetComponent<ZombieAI>();
        }

        private void LateUpdate()
        {
            if (brain == null) return;
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
    }
}

using UnityEngine;
using OutpostZero.AI;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Soft budget for the prototype arena: frame time and alive zombies.
    /// Logs once when the scene spends several frames over the cap.
    /// </summary>
    public class PerfBudget : MonoBehaviour
    {
        public const float FrameBudgetMs = 16.6f;
        public const int ZombieBudget = 32;

        private int overFrames;
        private bool reported;

        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            int zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None).Length;
            if (ms > FrameBudgetMs * 1.5f || zombies > ZombieBudget) overFrames++;
            else overFrames = 0;
            if (!reported && overFrames > 90)
            {
                reported = true;
                Debug.LogWarning($"[PerfBudget] Frame {ms:0.0} ms, zombies {zombies}. Budget is {FrameBudgetMs} ms and {ZombieBudget} alive.");
            }
        }
    }
}

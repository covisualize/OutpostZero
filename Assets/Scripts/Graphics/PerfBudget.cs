using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;

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
            var tier = QualityProfile.For(SettingsService.Instance != null ? SettingsService.Instance.Quality : 1);
            float ms = Time.unscaledDeltaTime * 1000f;
            int zombies = ZombieAI.AliveCount();
            if (ms > tier.FrameMs * 1.5f || zombies > tier.Zombies) overFrames++;
            else overFrames = 0;
            if (!reported && overFrames > 90)
            {
                reported = true;
                Debug.LogWarning($"[PerfBudget] Frame {ms:0.0} ms, zombies {zombies}. Budget is {tier.FrameMs} ms and {tier.Zombies} alive.");
            }
        }
    }
}

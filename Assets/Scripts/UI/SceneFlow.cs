using System;
using System.Collections;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// Fades through a loading card before camp, the street, the menu, or a fresh outpost.
    /// The card uses unscaled time so it still finishes while the game is paused.
    /// </summary>
    public class SceneFlow : MonoBehaviour
    {
        public static SceneFlow Instance { get; private set; }

        private LoadCard card;
        private bool booted;

        public bool Busy { get; private set; }
        public FlowStep Current { get; private set; } = FlowStep.Boot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private IEnumerator Start()
        {
            bool openOnMenu = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ExpeditionActive;
            if (openOnMenu) Time.timeScale = 0f;
            yield return null;
            BuildOverlay();
            if (booted) yield break;
            booted = true;
            if (openOnMenu) Travel(FlowStep.MainMenu);
        }

        /// <summary>With no arrival callback the entry hooks settle the step, so the game state follows the flow.</summary>
        public void Travel(FlowStep step, Action arrived = null)
        {
            if (Busy) return;
            StartCoroutine(Run(step, arrived));
        }

        private IEnumerator Run(FlowStep step, Action arrived)
        {
            Busy = true;
            Time.timeScale = 0f;
            Present(step, 0f);
            float[] beats = SceneRoute.Beats(step);
            for (int i = 0; i < beats.Length; i++)
            {
                Present(step, beats[i]);
                yield return new WaitForSecondsRealtime(0.28f);
            }
            arrived?.Invoke();
            if (step == FlowStep.MainMenu && Current == FlowStep.Boot && BootLoader.StartedAt >= 0f)
            {
                float cold = Time.realtimeSinceStartup;
                if (BootPlan.InBudget(cold)) Debug.Log("Cold start reached the menu in " + cold.ToString("0.00") + " s");
                else Debug.LogWarning("Cold start took " + cold.ToString("0.00") + " s, over the " + BootPlan.Budget + " s budget");
            }
            var from = Current;
            Current = step;
            SceneEntries.Dispatch(new FlowContext(from, step, arrived != null), Debug.LogException);
            yield return new WaitForSecondsRealtime(0.12f);
            card?.Hide();
            Busy = false;
        }

        private void Present(FlowStep step, float progress)
        {
            card?.Show(step, progress);
        }

        private void BuildOverlay()
        {
            card = LoadCard.Build(gameObject, 40);
        }
    }
}

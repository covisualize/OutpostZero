using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// Lives in Boot.unity. Streams the outpost scene behind the loading card, then hands
    /// off; the outpost's SceneFlow opens the main menu once its services are installed.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        public static float StartedAt { get; private set; } = -1f;

        private IEnumerator Start()
        {
            StartedAt = Time.realtimeSinceStartup;
            Time.timeScale = 1f;
            var card = LoadCard.Build(gameObject, 50);
            card?.Show(FlowStep.Boot, 0f);
            yield return null;

            var load = SceneManager.LoadSceneAsync(BootPlan.SceneFor(FlowStep.MainMenu), LoadSceneMode.Single);
            if (load == null) yield break;
            load.allowSceneActivation = false;
            while (!BootPlan.Loaded(load.progress))
            {
                card?.Show(FlowStep.Boot, BootPlan.Bar(load.progress));
                yield return null;
            }
            card?.Show(FlowStep.Boot, 1f);
            load.allowSceneActivation = true;
        }
    }
}

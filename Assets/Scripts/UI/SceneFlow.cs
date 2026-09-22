using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
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

        private UIDocument document;
        private VisualElement veil;
        private Label title;
        private Label tip;
        private Label progressLabel;
        private VisualElement fill;
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
            if (openOnMenu) Travel(FlowStep.MainMenu, () => GameManager.Instance.SetState(GameState.MainMenu));
        }

        public void Travel(FlowStep step, Action arrived)
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
            Current = step;
            yield return new WaitForSecondsRealtime(0.12f);
            if (veil != null) veil.style.display = DisplayStyle.None;
            Busy = false;
        }

        private void Present(FlowStep step, float progress)
        {
            if (veil == null) return;
            veil.style.display = DisplayStyle.Flex;
            title.text = SceneRoute.Title(step);
            int tipIndex = Mathf.FloorToInt(progress * 3f);
            tip.text = SceneRoute.Tip(step, tipIndex);
            progressLabel.text = Mathf.RoundToInt(progress * 100f) + "%";
            fill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }

        private void BuildOverlay()
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.sortingOrder = 40;
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            var root = document.rootVisualElement;
            if (root == null) return;

            veil = new VisualElement();
            veil.style.position = Position.Absolute;
            veil.style.left = 0;
            veil.style.top = 0;
            veil.style.right = 0;
            veil.style.bottom = 0;
            veil.style.backgroundColor = new Color(0.04f, 0.04f, 0.045f, 0.94f);
            veil.style.alignItems = Align.Center;
            veil.style.justifyContent = Justify.Center;
            veil.style.display = DisplayStyle.None;
            veil.pickingMode = PickingMode.Position;

            title = new Label();
            title.style.fontSize = 28;
            title.style.color = new Color(0.93f, 0.9f, 0.82f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 12;
            veil.Add(title);

            tip = new Label();
            tip.style.fontSize = 16;
            tip.style.whiteSpace = WhiteSpace.Normal;
            tip.style.width = 520;
            tip.style.color = new Color(0.78f, 0.74f, 0.66f);
            tip.style.unityTextAlign = TextAnchor.MiddleCenter;
            tip.style.marginBottom = 18;
            veil.Add(tip);

            var track = new VisualElement();
            track.style.width = 360;
            track.style.height = 8;
            track.style.backgroundColor = new Color(0.16f, 0.15f, 0.14f);
            fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(0);
            fill.style.backgroundColor = new Color(0.72f, 0.55f, 0.28f);
            track.Add(fill);
            veil.Add(track);

            progressLabel = new Label();
            progressLabel.style.marginTop = 8;
            progressLabel.style.fontSize = 13;
            progressLabel.style.color = new Color(0.62f, 0.58f, 0.5f);
            veil.Add(progressLabel);

            root.Add(veil);
        }
    }
}

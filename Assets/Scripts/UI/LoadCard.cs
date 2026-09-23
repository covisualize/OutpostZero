using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// The loading card: a title, a tip, and a bar. Boot and every in-game travel draw the same one.
    /// </summary>
    public class LoadCard
    {
        private readonly VisualElement veil;
        private readonly Label title;
        private readonly Label tip;
        private readonly Label progressLabel;
        private readonly VisualElement fill;

        private LoadCard(VisualElement veil, Label title, Label tip, Label progressLabel, VisualElement fill)
        {
            this.veil = veil;
            this.title = title;
            this.tip = tip;
            this.progressLabel = progressLabel;
            this.fill = fill;
        }

        public static LoadCard Build(GameObject host, int sortingOrder)
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.sortingOrder = sortingOrder;
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            PanelScale.Track(panel);
            var root = document.rootVisualElement;
            if (root == null) return null;

            var veil = new VisualElement();
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

            var title = new Label();
            title.style.fontSize = 28;
            title.style.color = new Color(0.93f, 0.9f, 0.82f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 12;
            veil.Add(title);

            var tip = new Label();
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
            var fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(0);
            fill.style.backgroundColor = new Color(0.72f, 0.55f, 0.28f);
            track.Add(fill);
            veil.Add(track);

            var progressLabel = new Label();
            progressLabel.style.marginTop = 8;
            progressLabel.style.fontSize = 13;
            progressLabel.style.color = new Color(0.62f, 0.58f, 0.5f);
            veil.Add(progressLabel);

            root.Add(veil);
            return new LoadCard(veil, title, tip, progressLabel, fill);
        }

        public void Show(FlowStep step, float progress)
        {
            veil.style.display = DisplayStyle.Flex;
            title.text = SceneRoute.Title(step, null);
            int tipIndex = Mathf.FloorToInt(progress * 3f);
            tip.text = SceneRoute.Tip(step, tipIndex, null);
            progressLabel.text = Mathf.RoundToInt(progress * 100f) + "%";
            fill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }

        public void Hide()
        {
            veil.style.display = DisplayStyle.None;
        }
    }
}

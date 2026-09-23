using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// F9 dev menu for the editor and development builds: jump between flow steps,
    /// toggle god mode, spawn zombies, hand out a kit, and skip the clock. F4 shows the roster sheet.
    /// </summary>
    public class DevPanel : MonoBehaviour
    {
        private VisualElement panel;
        private Label godLabel;
        private bool open;
        private VisualElement roster;
        private VisualElement rosterRows;
        private bool rosterOpen;
        private SurvivorRoster watched;

        private void Start()
        {
            if (!DevCheats.Allowed(Application.isEditor, Debug.isDebugBuild))
            {
                enabled = false;
                return;
            }
            Build();
        }

        private void Update()
        {
            if (ExpeditionInput.RosterPressed)
            {
                rosterOpen = DevCheats.Toggle(true, rosterOpen);
                if (roster != null) roster.style.display = rosterOpen ? DisplayStyle.Flex : DisplayStyle.None;
                RefreshRoster();
            }
            if (!ExpeditionInput.DevPressed) return;
            open = DevCheats.Toggle(true, open);
            if (panel != null) panel.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            Refresh();
        }

        private void OnDestroy()
        {
            if (watched != null) watched.OnRosterChanged -= RefreshRoster;
        }

        private void BuildRoster(VisualElement root)
        {
            roster = new VisualElement();
            roster.style.position = Position.Absolute;
            roster.style.left = 16;
            roster.style.top = 16;
            roster.style.maxWidth = 900;
            roster.style.paddingLeft = 10;
            roster.style.paddingRight = 10;
            roster.style.paddingTop = 8;
            roster.style.paddingBottom = 8;
            roster.style.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 0.92f);
            roster.style.display = DisplayStyle.None;
            var title = new Label(Loc.T("dev.roster"));
            title.style.color = new Color(0.95f, 0.75f, 0.3f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            roster.Add(title);
            rosterRows = new VisualElement();
            roster.Add(rosterRows);
            root.Add(roster);
        }

        private void RefreshRoster()
        {
            if (rosterRows == null || !rosterOpen) return;
            var service = SurvivorRoster.Instance;
            if (service != watched)
            {
                if (watched != null) watched.OnRosterChanged -= RefreshRoster;
                watched = service;
                if (watched != null) watched.OnRosterChanged += RefreshRoster;
            }
            rosterRows.Clear();
            if (service == null) return;
            foreach (var line in RosterSheet.Lines(service.Survivors, null))
            {
                var row = new Label(line);
                row.style.color = Color.white;
                row.style.whiteSpace = WhiteSpace.Normal;
                row.style.marginBottom = 2;
                rosterRows.Add(row);
            }
        }

        private void Build()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.sortingOrder = 45;
            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            PanelScale.Track(settings);
            var root = document.rootVisualElement;
            if (root == null) return;
            FontFallback.Dress(root);

            panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.right = 16;
            panel.style.top = 16;
            panel.style.width = 260;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.paddingTop = 8;
            panel.style.paddingBottom = 8;
            panel.style.backgroundColor = new Color(0.08f, 0.09f, 0.1f, 0.92f);
            panel.style.display = DisplayStyle.None;

            var title = new Label(Loc.T("dev.title"));
            title.style.color = new Color(0.95f, 0.75f, 0.3f);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            panel.Add(title);

            godLabel = new Label();
            godLabel.style.color = Color.white;
            panel.Add(godLabel);
            panel.Add(Button(Loc.T("dev.god"), () => { DevCheats.SetGod(!DevCheats.God); Refresh(); }));
            panel.Add(Button(Loc.T("dev.spawn") + " " + DevCheats.SpawnCount, Spawn));
            panel.Add(Button(Loc.T("dev.kit"), GiveKit));
            panel.Add(Button(Loc.T("dev.skip") + " " + DevCheats.SkipHours + "h", () => WorldClock.Instance?.Advance(DevCheats.SkipHours)));
            panel.Add(Button(Loc.T("dev.menu"), () => Jump(FlowStep.MainMenu)));
            panel.Add(Button(Loc.T("dev.camp"), () => Jump(FlowStep.Sanctuary)));
            panel.Add(Button(Loc.T("dev.street"), () => Jump(FlowStep.Expedition)));
            panel.Add(Button(Loc.T("dev.boot"), () => GameManager.Instance?.ReturnToBoot()));
            root.Add(panel);
            BuildRoster(root);
            Refresh();
        }

        private void Refresh()
        {
            if (godLabel != null) godLabel.text = Loc.T("dev.god") + ": " + (DevCheats.God ? Loc.T("dev.on") : Loc.T("dev.off"));
        }

        private static void Spawn()
        {
            var spawner = FindFirstObjectByType<ZombieSpawner>();
            spawner?.SpawnZombies(DevCheats.SpawnCount);
        }

        private static void GiveKit()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (inventory == null) return;
            foreach (var id in DevCheats.Kit)
            {
                var item = ItemCatalog.Find(id);
                if (item != null) inventory.TryAddItem(item.Id, item.DisplayName, item.Category, 1, item.Weight);
            }
            GameplayFeedback.Toast(Loc.T("dev.kit"));
        }

        private static void Jump(FlowStep step, System.Action arrived = null)
        {
            if (SceneFlow.Instance != null)
            {
                SceneFlow.Instance.Travel(step, arrived);
                return;
            }
            if (arrived != null) arrived();
            else GameManager.Instance?.Arrive(new FlowContext(step, step, false));
        }

        private static Button Button(string text, System.Action action)
        {
            var button = new Button(action) { text = text };
            button.style.height = 26;
            button.style.marginTop = 3;
            button.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            button.style.color = Color.white;
            return button;
        }
    }
}

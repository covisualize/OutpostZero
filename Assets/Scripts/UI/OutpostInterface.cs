using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    /// <summary>
    /// Runtime UI Toolkit surface for the HUD, pack, camp, and menus.
    /// Refreshes on unscaled time so pause, succession, and the main menu stay interactive.
    /// </summary>
    public class OutpostInterface : MonoBehaviour
    {
        private UIDocument document;
        private VisualElement root;
        private VisualElement menu;
        private VisualElement camp;
        private VisualElement pack;
        private VisualElement damageLayer;
        private Label vitals;
        private Label objectives;
        private Label weapon;
        private Label tutorial;
        private Label toast;
        private VisualElement noiseFill;
        private VisualElement healthFill;
        private string menuKey = "";
        private string campKey = "";
        private string packKey = "";
        private readonly List<Label> popups = new List<Label>();
        private int listening = -1;

        private void Update()
        {
            if (listening < 0 || Keyboard.current == null) return;
            foreach (Key key in (Key[])System.Enum.GetValues(typeof(Key)))
            {
                if (key == Key.None) continue;
                var control = Keyboard.current[key];
                if (control == null || !control.wasPressedThisFrame) continue;
                if (key != Key.Escape) ControlBindings.TryRebind((ControlBindings.Action)listening, key);
                listening = -1;
                menuKey = "";
                return;
            }
        }

        private void Start()
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.sortingOrder = 20;
            document = gameObject.GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            root = document.rootVisualElement;
            if (root == null) return;

            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Position;

            var left = Column(16, 16, 420);
            vitals = Body();
            objectives = Body();
            healthFill = Bar();
            left.Add(vitals);
            left.Add(healthFill);
            left.Add(objectives);
            root.Add(left);

            var bottom = Column(16, 0, 420);
            bottom.style.bottom = 16;
            bottom.style.top = StyleKeyword.Auto;
            weapon = Body();
            noiseFill = Bar();
            bottom.Add(weapon);
            bottom.Add(noiseFill);
            root.Add(bottom);

            tutorial = Body();
            tutorial.style.position = Position.Absolute;
            tutorial.style.bottom = 72;
            tutorial.style.left = Length.Percent(20);
            tutorial.style.width = Length.Percent(60);
            tutorial.style.unityTextAlign = TextAnchor.MiddleCenter;
            tutorial.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
            tutorial.pickingMode = PickingMode.Ignore;
            root.Add(tutorial);

            toast = Body();
            toast.style.position = Position.Absolute;
            toast.style.top = 80;
            toast.style.left = Length.Percent(30);
            toast.style.width = Length.Percent(40);
            toast.style.unityTextAlign = TextAnchor.MiddleCenter;
            toast.style.backgroundColor = new Color(0.12f, 0.1f, 0.08f, 0.9f);
            toast.pickingMode = PickingMode.Ignore;
            root.Add(toast);

            damageLayer = new VisualElement();
            damageLayer.pickingMode = PickingMode.Ignore;
            damageLayer.style.position = Position.Absolute;
            damageLayer.style.left = 0;
            damageLayer.style.top = 0;
            damageLayer.style.right = 0;
            damageLayer.style.bottom = 0;
            root.Add(damageLayer);

            menu = Overlay();
            camp = Overlay();
            camp.style.left = StyleKeyword.Auto;
            camp.style.right = 16;
            camp.style.width = 420;
            camp.style.top = 16;
            pack = Overlay();
            pack.style.left = Length.Percent(30);
            pack.style.width = 420;
            pack.style.top = 120;
            root.Add(menu);
            root.Add(camp);
            root.Add(pack);

            StartCoroutine(RefreshLoop());
        }

        private IEnumerator RefreshLoop()
        {
            var wait = new WaitForSecondsRealtime(0.05f);
            while (true)
            {
                Refresh();
                yield return wait;
            }
        }

        private void Refresh()
        {
            if (root == null) return;
            float scale = SettingsService.Instance != null ? SettingsService.Instance.TextScale : 1f;
            root.style.fontSize = Mathf.RoundToInt(14 * scale);
            var player = PlayerRegistry.Current;
            var hud = FindFirstObjectByType<SurvivalHUD>();
            var life = player != null ? player.GetComponent<HealthSystem>() : null;
            var needs = player != null ? player.GetComponent<SurvivalNeeds>() : null;
            var effects = player != null ? player.GetComponent<StatusEffectController>() : null;
            var visibility = player != null ? player.GetComponent<PlayerVisibility>() : null;
            var tracker = ObjectiveTracker.Instance;

            var vitalText = new StringBuilder();
            vitalText.AppendLine(Loc.T("hud.expedition"));
            if (life != null)
            {
                vitalText.AppendLine(Loc.T("hud.health") + " " + Mathf.CeilToInt(life.CurrentHealth) + " / " + Mathf.CeilToInt(life.MaxHealth));
                healthFill.style.width = Length.Percent(100f * life.CurrentHealth / Mathf.Max(1f, life.MaxHealth));
            }
            if (player != null) vitalText.AppendLine(Loc.T("hud.stamina") + " " + Mathf.CeilToInt(player.CurrentStamina));
            if (needs != null)
            {
                vitalText.AppendLine("Hunger " + Mathf.RoundToInt(needs.Hunger) + "  Thirst " + Mathf.RoundToInt(needs.Thirst) + "  Fatigue " + Mathf.RoundToInt(needs.Fatigue));
            }
            if (visibility != null) vitalText.Append("Exposure " + Mathf.RoundToInt(visibility.Exposure * 100f) + "%");
            if (effects != null)
            {
                if (effects.IsBleeding) vitalText.Append("  Bleeding");
                if (effects.IsPoisoned) vitalText.Append("  Poison");
                if (effects.IsInfected) vitalText.Append("  Infection");
            }
            var services = CampServices.Instance;
            if (services != null && services.Contacts > 0) vitalText.Append("  Watchtower " + services.Contacts);
            vitals.text = vitalText.ToString();

            var objectiveText = new StringBuilder();
            if (tracker != null) objectiveText.AppendLine("Kills " + tracker.Kills + "/" + tracker.KillGoal + "   Scrap " + tracker.Scrap + "/" + tracker.ScrapGoal);
            var district = WorldMapService.Instance != null ? WorldMapService.Instance.Current : null;
            if (district != null) objectiveText.AppendLine(district.displayName + " — " + district.encounter);
            if (HordeDirector.Instance != null) objectiveText.AppendLine("Tension " + Mathf.RoundToInt(HordeDirector.Instance.Tension) + "  " + HordeDirector.Instance.State);
            if (WorldClock.Instance != null) objectiveText.AppendLine(WorldClock.Instance.Label);
            var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            if (interactor != null && !string.IsNullOrEmpty(interactor.Prompt)) objectiveText.Append("[E] " + interactor.Prompt);
            var raid = NightRaidController.Instance;
            if (raid != null && raid.Running) objectiveText.Append("   Raid " + Mathf.CeilToInt(raid.Remaining) + "s");
            objectives.text = objectiveText.ToString();

            if (player != null && player.ActiveWeapon is FirearmWeapon gun)
            {
                weapon.text = gun.WeaponName + "   " + gun.CurrentAmmo + " / " + gun.ReserveAmmo + (gun.IsReloading ? "  reloading" : "");
            }
            else if (player != null && player.ActiveWeapon != null)
            {
                weapon.text = player.ActiveWeapon.WeaponName;
            }
            else weapon.text = "No weapon";

            float noise = hud != null ? hud.NoiseLevel : 0f;
            noiseFill.style.width = Length.Percent(noise * 100f);
            noiseFill.style.backgroundColor = Color.Lerp(new Color(0.2f, 0.7f, 0.3f), new Color(0.8f, 0.15f, 0.1f), noise);
            toast.text = hud != null ? hud.Toast ?? "" : "";
            toast.style.display = string.IsNullOrEmpty(toast.text) ? DisplayStyle.None : DisplayStyle.Flex;

            var tutorialDirector = TutorialDirector.Instance;
            bool showTutorial = tutorialDirector != null && !tutorialDirector.Finished && !string.IsNullOrEmpty(tutorialDirector.Current)
                && (SettingsService.Instance == null || SettingsService.Instance.Subtitles);
            tutorial.text = showTutorial ? tutorialDirector.Current : "";
            tutorial.style.display = showTutorial ? DisplayStyle.Flex : DisplayStyle.None;

            var shell = FindFirstObjectByType<GameShellUI>();
            bool inventory = shell != null && shell.InventoryOpen;
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            bool settings = SettingsService.Instance != null && SettingsService.Instance.ShowSettings;
            bool trade = FactionTrade.Instance != null && FactionTrade.Instance.Open;
            string language = SettingsService.Instance != null ? SettingsService.Instance.Language : "en";
            string discrete = SettingsService.Instance != null ? SettingsService.Instance.DiscreteKey : "";
            string nextMenu = state + "|" + settings + "|" + trade + "|" + language + "|" + discrete + "|" + ControlBindings.Signature() + "|" + listening;
            if (nextMenu != menuKey)
            {
                menuKey = nextMenu;
                RebuildMenu(state, settings, trade);
            }

            string nextCamp = state == GameState.CampManagement ? CampSignature() : "";
            if (nextCamp != campKey)
            {
                campKey = nextCamp;
                RebuildCamp(state == GameState.CampManagement);
            }

            string nextPack = inventory ? PackSignature() : "";
            if (nextPack != packKey)
            {
                packKey = nextPack;
                RebuildPack(inventory);
            }

            DrawPopups();
        }

        private void RebuildMenu(GameState state, bool settings, bool trade)
        {
            menu.Clear();
            if (settings)
            {
                BuildSettings(menu);
                return;
            }
            if (trade && FactionTrade.Instance != null)
            {
                var faction = FactionTrade.Instance;
                menu.Add(Title(faction.Faction + "  " + faction.Standing));
                menu.Add(Button("Buy medkit (" + faction.Price("medkit") + ")", () => faction.Buy("medkit")));
                menu.Add(Button("Buy rifle ammo (" + faction.Price("ammo_rifle") + ")", () => faction.Buy("ammo_rifle")));
                menu.Add(Button("Buy water (" + faction.Price("water") + ")", () => faction.Buy("water")));
                menu.Add(Button("Leave", faction.Toggle));
                return;
            }

            switch (state)
            {
                case GameState.Paused:
                    menu.Add(Title(Loc.T("menu.pause")));
                    menu.Add(Button(Loc.T("menu.resume"), () => GameManager.Instance.TogglePause()));
                    menu.Add(Button(Loc.T("menu.save"), () => SaveSystem.Instance?.Save()));
                    menu.Add(Button(Loc.T("menu.camp"), () => GameManager.Instance.EnterCamp()));
                    menu.Add(Button(Loc.T("menu.settings"), () => SettingsService.Instance?.TogglePanel()));
                    menu.Add(Button(Loc.T("menu.main"), () => GameManager.Instance.SetState(GameState.MainMenu)));
                    menu.Add(Button("Restart", () => GameManager.Instance.RestartCurrentScene()));
                    break;
                case GameState.SuccessionScreen:
                    menu.Add(Title("LEADER KILLED"));
                    menu.Add(Body("Choose who walks out next."));
                    if (SurvivorRoster.Instance != null)
                    {
                        foreach (var survivor in SurvivorRoster.Instance.Survivors)
                        {
                            if (!survivor.alive) continue;
                            string id = survivor.id;
                            menu.Add(Button(survivor.displayName + " — " + survivor.trait + "  " + survivor.bond, () => GameManager.Instance.AcceptSuccessor(id)));
                        }
                    }
                    menu.Add(Button("The outpost falls", () => GameManager.Instance.SetState(GameState.GameOver)));
                    break;
                case GameState.Victory:
                    menu.Add(Title("OUTPOST HOLDS"));
                    menu.Add(Body("Three districts are quiet. The gate can stay shut."));
                    menu.Add(Button("Enter sanctuary", () => GameManager.Instance.EnterCamp()));
                    menu.Add(Button("New outpost", () => GameManager.Instance.RestartCurrentScene()));
                    break;
                case GameState.ExpeditionResults:
                    menu.Add(Title(Loc.T("result.title")));
                    var map = WorldMapService.Instance;
                    menu.Add(Body(map != null && map.CampaignWon ? "The district ring is clear." : "Supplies are back inside the gate."));
                    if (map != null && map.Current != null) menu.Add(Body("Next: " + map.Current.displayName));
                    menu.Add(Button("Enter sanctuary", () => GameManager.Instance.EnterCamp()));
                    break;
                case GameState.GameOver:
                    menu.Add(Title(Loc.T("gameover.title")));
                    menu.Add(Body("Every name on the roster is gone."));
                    menu.Add(Button("New outpost", () => GameManager.Instance.RestartCurrentScene()));
                    break;
                case GameState.MainMenu:
                    menu.Add(Title("OUTPOST ZERO"));
                    menu.Add(Button("Continue expedition", () =>
                    {
                        if (SaveSystem.Instance != null && SaveSystem.Instance.Load()) return;
                        GameManager.Instance.SetState(GameState.ExpeditionActive);
                    }));
                    menu.Add(Button("New expedition", () => GameManager.Instance.RestartCurrentScene()));
                    menu.Add(Button(Loc.T("menu.settings"), () => SettingsService.Instance?.TogglePanel()));
                    menu.Add(Button("Back to the street", () => GameManager.Instance.SetState(GameState.ExpeditionActive)));
                    break;
            }
            menu.style.display = menu.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BuildSettings(VisualElement parent)
        {
            var settings = SettingsService.Instance;
            parent.Add(Title("SETTINGS"));
            parent.Add(SliderRow("Shake", settings.ScreenShake, settings.SetShake));
            parent.Add(SliderRow("Volume", settings.MasterVolume, settings.SetVolume));
            parent.Add(SliderRow("Effects", settings.SfxVolume, settings.SetSfx));
            parent.Add(SliderRow("Music", settings.MusicVolume, settings.SetMusic));
            parent.Add(SliderRow("Field of view", settings.FieldOfView, 40f, 75f, settings.SetFieldOfView));
            parent.Add(SliderRow("Text", settings.TextScale, 0.8f, 1.6f, settings.SetTextScale));
            parent.Add(Button(settings.Subtitles ? "Subtitles on" : "Subtitles off", () => settings.SetSubtitles(!settings.Subtitles)));
            parent.Add(Button("Colorblind mode " + settings.ColorblindMode, settings.CycleColorblind));
            parent.Add(Button(settings.Language == "es" ? "Idioma: ES" : "Language: EN", () => settings.SetLanguage(settings.Language == "es" ? "en" : "es")));
            string[] tiers = { "Low", "Medium", "High" };
            parent.Add(Button("Quality: " + tiers[Mathf.Clamp(settings.Quality, 0, 2)], settings.CycleQuality));
            parent.Add(Button(settings.VSync ? "VSync on" : "VSync off", settings.ToggleVSync));
            parent.Add(Body("Click an action, then press a key. Escape cancels."));
            for (int i = 0; i < ControlBindings.Count; i++)
            {
                var action = (ControlBindings.Action)i;
                int index = i;
                string caption = listening == index ? "Press a key for " + action : action + ": " + ControlBindings.Label(action);
                parent.Add(Button(caption, () => listening = index));
            }
            parent.Add(Button("Reset keys", () =>
            {
                ControlBindings.ResetDefaults();
                listening = -1;
                menuKey = "";
            }));
            parent.Add(Button("Close", settings.TogglePanel));
            parent.style.display = DisplayStyle.Flex;
        }

        private void RebuildCamp(bool open)
        {
            camp.Clear();
            camp.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open) return;
            camp.Add(Title(Loc.T("camp.title")));
            var storage = ColonyStorage.Instance;
            var services = CampServices.Instance;
            if (storage != null)
            {
                camp.Add(Body("Scrap " + storage.Scrap + "  Food " + storage.Food + "  Water " + storage.Water
                    + (services != null && services.GeneratorOnline ? "  Generator on" : "  Generator dark")));
            }
            var roster = SurvivorRoster.Instance;
            if (roster != null)
            {
                camp.Add(Body("Morale " + Mathf.RoundToInt(roster.AverageMorale())));
                if (!string.IsNullOrEmpty(roster.DayNotes)) camp.Add(Body(roster.DayNotes));
                foreach (var survivor in roster.Survivors)
                {
                    string flag = survivor.leader ? "*" : survivor.alive ? "" : "x";
                    string mood = ColonyDay.Mood(survivor.morale);
                    camp.Add(Body(flag + " " + survivor.displayName + " (" + survivor.trait + ") " + survivor.task
                        + "  " + mood
                        + "  food " + Mathf.RoundToInt(survivor.hunger)
                        + " water " + Mathf.RoundToInt(survivor.thirst)
                        + "  " + survivor.bond
                        + "  opinion " + survivor.opinion));
                    if (!survivor.alive) continue;
                    string id = survivor.id;
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.Add(Button("Rest", () => roster.Assign(id, "Rest")));
                    row.Add(Button("Scavenge", () => roster.Assign(id, "Scavenge")));
                    row.Add(Button("Guard", () => roster.Assign(id, "Guard")));
                    row.Add(Button("Cook", () => roster.Assign(id, "Cook")));
                    row.Add(Button("Medic", () => roster.Assign(id, "Medic")));
                    camp.Add(row);
                }
            }
            camp.Add(Button("Advance watch", () =>
            {
                WorldClock.Instance?.Advance(6f);
                SurvivorRoster.Instance?.TickTasks();
            }));
            camp.Add(Button("Endure the night", () => NightRaidController.Instance?.Begin()));
            camp.Add(Body("Build [B] then click. " + (GridBuilder.Instance != null ? GridBuilder.Instance.Selected.ToString() : "")));
            var build = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            build.Add(Button("Barricade", () => GridBuilder.Instance?.Select(ModuleKind.Barricade)));
            build.Add(Button("Cot", () => GridBuilder.Instance?.Select(ModuleKind.Cot)));
            build.Add(Button("Water", () => GridBuilder.Instance?.Select(ModuleKind.Water)));
            build.Add(Button("Tower", () => GridBuilder.Instance?.Select(ModuleKind.Watchtower)));
            camp.Add(build);
            camp.Add(Body("Craft"));
            foreach (var recipe in CraftingBench.Recipes)
            {
                string id = recipe.Id;
                camp.Add(Button(recipe.Label + " (" + recipe.ScrapCost + ")", () => CraftingBench.Instance?.Craft(id)));
            }
            var map = WorldMapService.Instance;
            if (map != null)
            {
                camp.Add(Body("District"));
                foreach (var district in map.Districts)
                {
                    if (district.cleared)
                    {
                        camp.Add(Body(district.displayName + " — clear"));
                        continue;
                    }
                    string id = district.id;
                    string mark = map.Current != null && map.Current.id == id ? "> " : "";
                    camp.Add(Button(mark + district.displayName, () => map.Select(id)));
                }
            }
            camp.Add(Button("Leave for the district", () => GameManager.Instance.BeginExpedition()));
        }

        private void RebuildPack(bool open)
        {
            pack.Clear();
            pack.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open) return;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            pack.Add(Title("PACK"));
            if (inventory == null) return;
            pack.Add(Body("Weight " + inventory.CurrentWeight.ToString("0.0") + " / " + inventory.MaxWeightCapacity.ToString("0.0")));
            var scroll = new ScrollView();
            scroll.style.height = 280;
            foreach (var item in inventory.Items)
            {
                string id = item.ItemId;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var label = Body();
                label.text = item.ItemName + " x" + item.Quantity;
                label.style.flexGrow = 1;
                row.Add(label);
                row.Add(Button("Use", () => inventory.TryUse(id)));
                scroll.Add(row);
            }
            pack.Add(scroll);
            pack.Add(Button("Close", () => FindFirstObjectByType<GameShellUI>()?.CloseInventory()));
        }

        private void DrawPopups()
        {
            var feedback = HitFeedback.Instance;
            if (feedback == null || document.rootVisualElement.panel == null || Camera.main == null)
            {
                damageLayer.Clear();
                return;
            }
            feedback.PrunePopups();
            damageLayer.Clear();
            int mode = SettingsService.Instance != null ? SettingsService.Instance.ColorblindMode : 0;
            foreach (var popup in feedback.Popups)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(popup.World);
                if (screen.z < 0f) continue;
                Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(document.rootVisualElement.panel, popup.World, Camera.main);
                var label = new Label(mode == 2 && popup.Crit ? popup.Text + " !" : popup.Text);
                label.pickingMode = PickingMode.Ignore;
                label.style.position = Position.Absolute;
                label.style.left = panelPos.x;
                label.style.top = panelPos.y;
                label.style.color = popup.Crit
                    ? (mode == 1 ? new Color(0.3f, 0.7f, 1f) : new Color(1f, 0.85f, 0.2f))
                    : Color.white;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                damageLayer.Add(label);
            }
        }

        private static string CampSignature()
        {
            var builder = new StringBuilder();
            if (ColonyStorage.Instance != null) builder.Append(ColonyStorage.Instance.Scrap).Append(ColonyStorage.Instance.Food);
            if (SurvivorRoster.Instance != null)
            {
                foreach (var survivor in SurvivorRoster.Instance.Survivors)
                {
                    builder.Append(survivor.id).Append(survivor.task).Append(survivor.alive).Append(survivor.leader);
                    builder.Append(Mathf.RoundToInt(survivor.morale)).Append(Mathf.RoundToInt(survivor.hunger)).Append(survivor.opinion).Append(survivor.injury);
                }
                builder.Append(SurvivorRoster.Instance.DayNotes);
            }
            if (GridBuilder.Instance != null) builder.Append(GridBuilder.Instance.Selected);
            if (CampServices.Instance != null) builder.Append(CampServices.Instance.GeneratorOnline);
            if (WorldMapService.Instance != null && WorldMapService.Instance.Current != null)
            {
                builder.Append(WorldMapService.Instance.Current.id);
            }
            return builder.ToString();
        }

        private static string PackSignature()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (inventory == null) return "";
            var builder = new StringBuilder();
            foreach (var item in inventory.Items) builder.Append(item.ItemId).Append(item.Quantity);
            builder.Append(inventory.MedicalKits);
            return builder.ToString();
        }

        private static VisualElement Overlay()
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(30);
            element.style.top = Length.Percent(18);
            element.style.width = 460;
            element.style.paddingTop = 12;
            element.style.paddingBottom = 12;
            element.style.paddingLeft = 12;
            element.style.paddingRight = 12;
            element.style.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 0.92f);
            element.style.display = DisplayStyle.None;
            return element;
        }

        private static VisualElement Column(float left, float top, float width)
        {
            var element = new VisualElement();
            element.pickingMode = PickingMode.Ignore;
            element.style.position = Position.Absolute;
            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 0.72f);
            element.style.paddingTop = 8;
            element.style.paddingLeft = 8;
            element.style.paddingBottom = 8;
            return element;
        }

        private static Label Title(string text)
        {
            var label = Body();
            label.text = text;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 18;
            label.style.marginBottom = 8;
            return label;
        }

        private static Label Body()
        {
            var label = new Label();
            label.style.color = Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 4;
            return label;
        }

        private static VisualElement Bar()
        {
            var bar = new VisualElement();
            bar.style.height = 8;
            bar.style.width = Length.Percent(40);
            bar.style.backgroundColor = new Color(0.7f, 0.2f, 0.15f);
            bar.style.marginBottom = 6;
            return bar;
        }

        private static Button Button(string text, System.Action action)
        {
            var button = new Button(action) { text = text };
            button.style.height = 30;
            button.style.marginRight = 4;
            button.style.marginBottom = 4;
            button.style.backgroundColor = new Color(0.18f, 0.18f, 0.2f);
            button.style.color = Color.white;
            return button;
        }

        private static VisualElement SliderRow(string caption, float value, System.Action<float> set)
        {
            return SliderRow(caption, value, 0f, 1f, set);
        }

        private static VisualElement SliderRow(string caption, float value, float min, float max, System.Action<float> set)
        {
            var row = new VisualElement();
            row.Add(Body());
            ((Label)row[0]).text = caption + " " + value.ToString("0.00");
            var slider = new Slider(min, max) { value = value };
            slider.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue);
                ((Label)row[0]).text = caption + " " + evt.newValue.ToString("0.00");
            });
            row.Add(slider);
            return row;
        }
    }
}

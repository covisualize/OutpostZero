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
using OutpostZero.Graphics;
using OutpostZero.Items;
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
        private Label compass;
        private Label hurt;
        private Label feed;
        private VisualElement noiseFill;
        private VisualElement healthFill;
        private VisualElement ghostFill;
        private float ghostRatio = 1f;
        private string menuKey = "";
        private string campKey = "";
        private string packKey = "";
        private readonly List<Label> popups = new List<Label>();
        private int listening = -1;
        private bool credits;
        private bool slotsOpen;
        private bool codexOpen;
        private string codexId = "";

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
            var healthTrack = new VisualElement();
            healthTrack.style.height = 8;
            healthTrack.style.width = Length.Percent(40);
            healthTrack.style.marginBottom = 6;
            healthTrack.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            ghostFill = new VisualElement();
            ghostFill.style.position = Position.Absolute;
            ghostFill.style.left = 0;
            ghostFill.style.top = 0;
            ghostFill.style.height = 8;
            ghostFill.style.backgroundColor = new Color(0.45f, 0.18f, 0.14f);
            healthFill = new VisualElement();
            healthFill.style.position = Position.Absolute;
            healthFill.style.left = 0;
            healthFill.style.top = 0;
            healthFill.style.height = 8;
            healthFill.style.backgroundColor = new Color(0.75f, 0.2f, 0.16f);
            healthTrack.Add(ghostFill);
            healthTrack.Add(healthFill);
            left.Add(vitals);
            left.Add(healthTrack);
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

            compass = Body();
            compass.style.position = Position.Absolute;
            compass.style.top = 12;
            compass.style.left = Length.Percent(28);
            compass.style.width = Length.Percent(44);
            compass.style.unityTextAlign = TextAnchor.MiddleCenter;
            compass.pickingMode = PickingMode.Ignore;
            root.Add(compass);

            hurt = Body();
            hurt.style.position = Position.Absolute;
            hurt.style.top = Length.Percent(42);
            hurt.style.left = Length.Percent(36);
            hurt.style.width = Length.Percent(28);
            hurt.style.unityTextAlign = TextAnchor.MiddleCenter;
            hurt.style.backgroundColor = new Color(0.45f, 0.08f, 0.06f, 0.82f);
            hurt.pickingMode = PickingMode.Ignore;
            hurt.style.display = DisplayStyle.None;
            root.Add(hurt);

            feed = Body();
            feed.style.position = Position.Absolute;
            feed.style.top = 16;
            feed.style.right = 16;
            feed.style.width = 180;
            feed.style.unityTextAlign = TextAnchor.UpperRight;
            feed.style.whiteSpace = WhiteSpace.PreWrap;
            feed.pickingMode = PickingMode.Ignore;
            root.Add(feed);

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
            root.style.opacity = SettingsService.Instance != null ? SettingsService.Instance.HudOpacity : 1f;
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
                float actual = life.CurrentHealth / Mathf.Max(1f, life.MaxHealth);
                ghostRatio = HealthGhost.Follow(ghostRatio, actual, 0.05f);
                ghostFill.style.width = Length.Percent(ghostRatio * 100f);
                healthFill.style.width = Length.Percent(actual * 100f);
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
                if (effects.InfectionStage > 0) vitalText.Append("  " + Affliction.Label(effects.InfectionStage));
                if (effects.SprintBonus > 1f) vitalText.Append("  Adrenaline");
            }
            var services = CampServices.Instance;
            if (services != null && services.Contacts > 0) vitalText.Append("  Watchtower " + services.Contacts);
            vitals.text = vitalText.ToString();

            var objectiveText = new StringBuilder();
            if (tracker != null)
            {
                objectiveText.AppendLine("Kills " + tracker.Kills + "/" + tracker.KillGoal + "   Scrap " + tracker.Scrap + "/" + tracker.ScrapGoal);
                if (!string.IsNullOrEmpty(tracker.PoiLine())) objectiveText.AppendLine(tracker.PoiLine());
            }
            if (!string.IsNullOrEmpty(RescueFollower.Status())) objectiveText.AppendLine(RescueFollower.Status());
            var district = WorldMapService.Instance != null ? WorldMapService.Instance.Current : null;
            if (district != null) objectiveText.AppendLine(district.displayName + " — " + district.encounter);
            if (HordeDirector.Instance != null) objectiveText.AppendLine("Tension " + Mathf.RoundToInt(HordeDirector.Instance.Tension) + "  " + HordeDirector.Instance.State);
            if (WorldClock.Instance != null) objectiveText.AppendLine(WorldClock.Instance.Label);
            var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            if (interactor != null && !string.IsNullOrEmpty(interactor.Prompt)) objectiveText.Append("[E] " + interactor.Prompt);
            var raid = NightRaidController.Instance;
            if (raid != null && raid.Running) objectiveText.Append("   Raid " + Mathf.CeilToInt(raid.Remaining) + "s");
            var gate = ExtractionZone.Current;
            if (gate != null && gate.Holding) objectiveText.AppendLine("Hold to extract " + Mathf.CeilToInt(ExtractWatch.HoldSeconds - gate.Hold) + "s");
            objectives.text = objectiveText.ToString();

            var flow = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            bool street = flow == GameState.ExpeditionActive || flow == GameState.RaidActive;
            compass.style.display = street ? DisplayStyle.Flex : DisplayStyle.None;
            if (street && player != null)
            {
                var poi = FindFirstObjectByType<DistrictPoi>();
                bool showPoi = poi != null && (tracker == null || !tracker.PoiFound);
                var face = player.transform.forward;
                var at = player.transform.position;
                compass.text = StreetHeading.Readout(
                    face.x, face.z, at.x, at.z,
                    showPoi, showPoi ? poi.transform.position.x : 0f, showPoi ? poi.transform.position.z : 0f,
                    gate != null, gate != null ? gate.transform.position.x : 0f, gate != null ? gate.transform.position.z : 0f);
            }
            else compass.text = "";

            hurt.style.display = DisplayStyle.None;
            if (street && life != null && player != null && Time.time - life.LastHitTime < 1.2f)
            {
                var hit = life.LastHitDirection;
                if (hit.sqrMagnitude > 0.0001f)
                {
                    var face = player.transform.forward;
                    string sector = StreetHeading.Sector(StreetHeading.Incoming(face.x, face.z, hit.x, hit.z));
                    hurt.text = sector == "front" ? "Hit from the front"
                        : sector == "back" ? "Hit from behind"
                        : sector == "left" ? "Hit from the left"
                        : "Hit from the right";
                    hurt.style.display = DisplayStyle.Flex;
                }
            }

            feed.text = GameManager.Instance != null ? GameManager.Instance.KillFeed : "";
            feed.style.display = string.IsNullOrEmpty(feed.text) ? DisplayStyle.None : DisplayStyle.Flex;

            if (player != null && player.ActiveWeapon is FirearmWeapon gun)
            {
                weapon.text = gun.WeaponName + "   " + gun.CurrentAmmo + " / " + gun.ReserveAmmo + (gun.IsReloading ? "  reloading" : "");
            }
            else if (player != null && player.ActiveWeapon != null)
            {
                weapon.text = player.ActiveWeapon.WeaponName;
            }
            else weapon.text = "No weapon";
            var carried = player != null ? player.GetComponent<PlayerInventory>() : null;
            if (carried != null)
            {
                weapon.style.whiteSpace = WhiteSpace.PreWrap;
                weapon.text += "\n" + carried.BeltLine;
            }

            float noise = hud != null ? hud.NoiseLevel : 0f;
            noiseFill.style.width = Length.Percent(noise * 100f);
            int vision = SettingsService.Instance != null ? SettingsService.Instance.ColorblindMode : 0;
            if (vision == 1) noiseFill.style.backgroundColor = Color.Lerp(new Color(0.2f, 0.45f, 0.95f), new Color(0.95f, 0.85f, 0.15f), noise);
            else if (vision == 2) noiseFill.style.backgroundColor = Color.Lerp(new Color(0.1f, 0.1f, 0.1f), Color.white, noise);
            else noiseFill.style.backgroundColor = Color.Lerp(new Color(0.2f, 0.7f, 0.3f), new Color(0.8f, 0.15f, 0.1f), noise);
            noiseFill.style.height = vision == 2 ? 8f + noise * 10f : 6f;
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
            string tradeKey = FactionTrade.Instance != null ? FactionTrade.Instance.Signature : "";
            string nextMenu = state + "|" + settings + "|" + trade + "|" + language + "|" + discrete + "|" + ControlBindings.Signature() + "|" + listening + "|" + credits + "|" + slotsOpen + "|" + codexOpen + "|" + codexId + "|" + tradeKey;
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

        private void DrawCodex(VisualElement parent)
        {
            string packed = CodexDirector.Instance != null ? CodexDirector.Instance.Packed : "";
            if (!string.IsNullOrEmpty(codexId))
            {
                CodexBook.Entry selected = null;
                for (int i = 0; i < CodexBook.Entries.Length; i++)
                {
                    if (CodexBook.Entries[i].Id == codexId) selected = CodexBook.Entries[i];
                }
                parent.Add(Title(selected != null && CodexBook.Visible(selected, packed) ? selected.Title : "Unknown"));
                parent.Add(Body(selected != null && CodexBook.Visible(selected, packed) ? selected.Body : "Not seen yet."));
                parent.Add(Button("Back", () => codexId = ""));
                return;
            }
            parent.Add(Title("CODEX"));
            for (int i = 0; i < CodexBook.Entries.Length; i++)
            {
                var entry = CodexBook.Entries[i];
                string id = entry.Id;
                bool visible = CodexBook.Visible(entry, packed);
                parent.Add(Button(visible ? entry.Title : "Unknown", () => codexId = id));
            }
            parent.Add(Button("Close", () => codexOpen = false));
        }

        private static void Go(FlowStep step, System.Action arrived)
        {
            if (SceneFlow.Instance != null) SceneFlow.Instance.Travel(step, arrived);
            else arrived?.Invoke();
        }

        private static void DrawTrade(VisualElement parent, FactionTrade faction)
        {
            string id = faction.ActiveId;
            int standing = faction.StandingOf(id);
            parent.Add(Title(faction.Faction + "  " + standing));
            if (CaravanBook.Refuses(id, standing))
            {
                parent.Add(Body(faction.Faction + " will not trade"));
            }
            else
            {
                string[] stock = CaravanBook.Stock(id);
                for (int i = 0; i < stock.Length; i++)
                {
                    string itemId = stock[i];
                    var record = ItemCatalog.Find(itemId);
                    string label = record != null ? record.DisplayName : itemId;
                    parent.Add(Button("Buy " + label + " (" + faction.Price(itemId) + ")", () => faction.Buy(itemId)));
                }
                parent.Add(Button("Sell bandage", () => faction.SellBandage()));
            }
            parent.Add(Body(QuestLine(id, faction.Quests)));
            if (id == "clinic" && !CaravanBook.QuestDone(faction.Quests, "clinic"))
            {
                parent.Add(Button("Deliver 4 medkits", () => faction.DeliverMedkits()));
            }
            parent.Add(Button("Leave", faction.Toggle));
        }

        private static string QuestLine(string id, string quests)
        {
            if (id == "clinic") return CaravanBook.QuestDone(quests, "clinic") ? "Field dressings learned" : "The Clinic wants 4 medkits";
            if (id == "farmers") return CaravanBook.QuestDone(quests, "farmers") ? "Farmers remember the nest" : "Clear a district for the farmers";
            if (id == "militia") return "Iron Militia sells rifle and shell ammo";
            return CaravanBook.QuestDone(quests, "caravan") ? "Escort complete" : "Extract on a visit day and the caravan pays";
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
                DrawTrade(menu, FactionTrade.Instance);
                return;
            }

            switch (state)
            {
                case GameState.Paused:
                    if (codexOpen)
                    {
                        DrawCodex(menu);
                        break;
                    }
                    menu.Add(Title(Loc.T("menu.pause")));
                    menu.Add(Button(Loc.T("menu.resume"), () => GameManager.Instance.TogglePause()));
                    menu.Add(Button(Loc.T("menu.save"), () => SaveSystem.Instance?.Save()));
                    menu.Add(Button("Codex", () => { codexOpen = true; codexId = ""; }));
                    menu.Add(Button("Skip the lesson", () => TutorialDirector.Instance?.Dismiss()));
                    menu.Add(Button(Loc.T("menu.camp"), () => Go(FlowStep.Sanctuary, () => GameManager.Instance.EnterCamp())));
                    menu.Add(Button(Loc.T("menu.settings"), () => SettingsService.Instance?.TogglePanel()));
                    menu.Add(Button(Loc.T("menu.main"), () => Go(FlowStep.MainMenu, () => GameManager.Instance.SetState(GameState.MainMenu))));
                    menu.Add(Button("Restart", () => Go(FlowStep.Boot, () => GameManager.Instance.RestartCurrentScene())));
                    break;
                case GameState.SuccessionScreen:
                    menu.Add(Title("LEADER KILLED"));
                    if (SurvivorRoster.Instance != null && SurvivorRoster.Instance.Memorials.Count > 0)
                    {
                        var fallen = SurvivorRoster.Instance.Memorials[SurvivorRoster.Instance.Memorials.Count - 1];
                        menu.Add(Body(SuccessionLedger.Card(fallen)));
                    }
                    menu.Add(Body("Choose who walks out at dawn."));
                    if (SurvivorRoster.Instance != null)
                    {
                        foreach (var survivor in SurvivorRoster.Instance.Survivors)
                        {
                            if (!survivor.alive) continue;
                            string id = survivor.id;
                            menu.Add(Button(survivor.displayName + " — " + survivor.trait + "  " + ColonyDay.Mood(survivor.morale), () => GameManager.Instance.AcceptSuccessor(id)));
                        }
                    }
                    menu.Add(Button("The outpost falls", () => GameManager.Instance.SetState(GameState.GameOver)));
                    break;
                case GameState.Victory:
                    menu.Add(Title("OUTPOST HOLDS"));
                    menu.Add(Body("The broadcast went out. The gate can stay shut."));
                    DrawBoard(menu);
                    menu.Add(Button("Enter sanctuary", () => Go(FlowStep.Sanctuary, () => GameManager.Instance.EnterCamp())));
                    menu.Add(Button("Keep the nights", () => Go(FlowStep.Sanctuary, () =>
                    {
                        WorldMapService.Instance?.TryBeginEndless();
                        GameManager.Instance.EnterCamp();
                    })));
                    menu.Add(Button("New outpost", () => Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost())));
                    break;
                case GameState.ExpeditionResults:
                    menu.Add(Title(Loc.T("result.title")));
                    var map = WorldMapService.Instance;
                    menu.Add(Body(map != null && map.CampaignWon ? "The tower is on the air." : "Supplies are back inside the gate."));
                    if (map != null && map.Current != null) menu.Add(Body("Next: " + map.Current.displayName));
                    menu.Add(Button("Enter sanctuary", () => Go(FlowStep.Sanctuary, () => GameManager.Instance.EnterCamp())));
                    break;
                case GameState.GameOver:
                    menu.Add(Title(Loc.T("gameover.title")));
                    int remembered = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Memorials.Count : 0;
                    menu.Add(Body(remembered > 0 ? remembered + " names on the memorial wall." : "Every name on the roster is gone."));
                    DrawBoard(menu);
                    menu.Add(Button("New outpost", () => Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost())));
                    break;
                case GameState.MainMenu:
                    if (credits)
                    {
                        menu.Add(Title("CREDITS"));
                        menu.Add(Body("Outpost Zero " + SceneRoute.Version));
                        menu.Add(Body("A sanctuary, a street, and whoever is still on the board."));
                        menu.Add(Button("Back", () => credits = false));
                        break;
                    }
                    if (slotsOpen)
                    {
                        DrawSlots(menu);
                        break;
                    }
                    menu.Add(Title("OUTPOST ZERO"));
                    menu.Add(Body("Version " + SceneRoute.Version));
                    menu.Add(Button("Continue", () => Go(FlowStep.Sanctuary, () =>
                    {
                        if (SaveSystem.Instance == null || !SaveSystem.Instance.Load())
                            GameManager.Instance.SetState(GameState.MainMenu);
                    })));
                    menu.Add(Button("Saves", () => slotsOpen = true));
                    menu.Add(Button("Difficulty: " + DifficultyProfile.Name(SettingsService.Instance != null ? SettingsService.Instance.NextDifficulty : 2), () => SettingsService.Instance?.CycleDifficulty()));
                    menu.Add(Button("New outpost", () => Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost())));
                    menu.Add(Button(Loc.T("menu.settings"), () => SettingsService.Instance?.TogglePanel()));
                    menu.Add(Button("Credits", () => credits = true));
                    menu.Add(Button("Skip the lesson", () => TutorialDirector.Instance?.Dismiss()));
                    menu.Add(Button("Back to the street", () => Go(FlowStep.Expedition, () => GameManager.Instance.BeginExpedition())));
                    break;
            }
            menu.style.display = menu.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void DrawBoard(VisualElement menu)
        {
            menu.Add(Body("Local board"));
            var runs = RunArchive.Instance != null ? RunArchive.Instance.Runs : null;
            if (runs == null || runs.Length == 0)
            {
                menu.Add(Body("No finished runs yet."));
                return;
            }
            int count = runs.Length < 4 ? runs.Length : 4;
            for (int i = 0; i < count; i++) menu.Add(Body(RunBoard.Line(runs[i])));
        }

        private void BuildSettings(VisualElement parent)
        {
            var settings = SettingsService.Instance;
            parent.Add(Title("SETTINGS"));
            parent.Add(SliderRow("Shake", settings.ScreenShake, settings.SetShake));
            parent.Add(SliderRow("Volume", settings.MasterVolume, settings.SetVolume));
            parent.Add(SliderRow("Effects", settings.SfxVolume, settings.SetSfx));
            parent.Add(SliderRow("Music", settings.MusicVolume, settings.SetMusic));
            parent.Add(SliderRow("Ambience", settings.AmbienceVolume, settings.SetAmbience));
            parent.Add(SliderRow("Interface", settings.UiVolume, settings.SetUi));
            parent.Add(SliderRow("Field of view", settings.FieldOfView, 40f, 75f, settings.SetFieldOfView));
            parent.Add(SliderRow("Text", settings.TextScale, 0.8f, 1.6f, settings.SetTextScale));
            parent.Add(SliderRow("HUD", settings.HudOpacity, 0.45f, 1f, settings.SetHudOpacity));
            parent.Add(SliderRow("Brightness", settings.Brightness, 0.6f, 1.4f, settings.SetBrightness));
            parent.Add(Button(settings.Subtitles ? "Subtitles on" : "Subtitles off", () => settings.SetSubtitles(!settings.Subtitles)));
            parent.Add(Button("Colorblind mode " + settings.ColorblindMode, settings.CycleColorblind));
            parent.Add(Button(settings.Language == "es" ? "Idioma: ES" : "Language: EN", () => settings.SetLanguage(settings.Language == "es" ? "en" : "es")));
            string[] tiers = { "Low", "Medium", "High", "Ultra" };
            parent.Add(Button("Quality: " + tiers[Mathf.Clamp(settings.Quality, 0, 3)], settings.CycleQuality));
            parent.Add(Button(settings.VSync ? "VSync on" : "VSync off", settings.ToggleVSync));
            parent.Add(Button("Frame cap: " + PlayOptions.FrameName(settings.FrameCap), settings.CycleFrameCap));
            parent.Add(Button("Resolution: " + DisplayModes.Name(settings.Resolution), settings.CycleResolution));
            parent.Add(Button(settings.AimAssist == 0 ? "Aim assist off" : settings.AimAssist == 2 ? "Aim assist strong" : "Aim assist light", settings.CycleAim));
            parent.Add(Button(settings.InvertLook ? "Invert look" : "Look: normal", settings.ToggleInvert));
            parent.Add(Button(settings.CrouchMode == 1 ? "Crouch: toggle" : "Crouch: hold", settings.ToggleCrouchMode));
            parent.Add(Button(settings.SprintMode == 1 ? "Sprint: toggle" : "Sprint: hold", settings.ToggleSprintMode));
            parent.Add(Button(settings.Merciful ? "Death: merciful" : "Death: permadeath", settings.ToggleMerciful));
            parent.Add(Button("Next run: " + DifficultyProfile.Name(settings.NextDifficulty), settings.CycleDifficulty));
            parent.Add(Button("Gore: " + Presentation.GoreName(settings.Gore == 0 ? 3 : settings.Gore), settings.CycleGore));
            parent.Add(Button(settings.HitStop ? "Hit stop on" : "Hit stop off", settings.ToggleHitStop));
            parent.Add(Button(settings.DamageNumbers ? "Damage numbers on" : "Damage numbers off", settings.ToggleDamageNumbers));
            parent.Add(Button(settings.MotionBlur ? "Motion blur on" : "Motion blur off", settings.ToggleMotionBlur));
            parent.Add(Button(settings.WindowMode == 1 ? "Windowed" : settings.WindowMode == 2 ? "Fullscreen" : "Display: default", settings.CycleWindow));
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
                if (roster.Memorials.Count > 0)
                {
                    camp.Add(Body("Memorial wall"));
                    for (int i = 0; i < roster.Memorials.Count; i++) camp.Add(Body(SuccessionLedger.Card(roster.Memorials[i])));
                }
                foreach (var survivor in roster.Survivors)
                {
                    string flag = survivor.leader ? "*" : survivor.alive ? "" : "x";
                    string mood = ColonyDay.Mood(survivor.morale);
                    string doing = CampRoutine.Choose(survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury);
                    string post = doing == survivor.task ? survivor.task : survivor.task + " → " + doing;
                    camp.Add(Body(flag + " " + survivor.displayName + " (" + survivor.trait + ") " + post
                        + "  " + mood
                        + "  food " + Mathf.RoundToInt(survivor.hunger)
                        + " water " + Mathf.RoundToInt(survivor.thirst)
                        + "  " + survivor.bond
                        + "  opinion " + survivor.opinion
                        + "  \"" + CampRoutine.Bark(doing, survivor.morale) + "\""));
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
            if (FactionTrade.Instance != null)
            {
                var merchants = FactionTrade.Instance;
                string who = string.IsNullOrEmpty(merchants.ActiveId) ? "caravan" : merchants.ActiveId;
                string presence = string.IsNullOrEmpty(merchants.ActiveId) ? "away" : "at the gate";
                camp.Add(Body(CaravanBook.Display(who) + " — " + presence + "  " + merchants.StandingOf(who)));
            }
            camp.Add(Button("Caravan", () => FactionTrade.Instance?.Toggle()));
            camp.Add(Button("Advance watch", () =>
            {
                WorldClock.Instance?.Advance(6f);
                SurvivorRoster.Instance?.TickTasks();
            }));
            int raidDay = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int raidSecurity = ColonyStorage.Instance != null ? ColonyStorage.Instance.Security : 0;
            bool endlessNights = WorldMapService.Instance != null && WorldMapService.Instance.Endless;
            camp.Add(Body(RaidPlan.Due(raidDay, raidSecurity, endlessNights) ? "A raid is likely tonight." : "The street is quiet tonight."));
            camp.Add(Button("Endure the night", () => NightRaidController.Instance?.Begin()));
            camp.Add(Body("Build [B] then click. " + (GridBuilder.Instance != null ? GridBuilder.Instance.Selected.ToString() : "")));
            var build = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            build.Add(Button("Barricade", () => GridBuilder.Instance?.Select(ModuleKind.Barricade)));
            build.Add(Button("Cot", () => GridBuilder.Instance?.Select(ModuleKind.Cot)));
            build.Add(Button("Water", () => GridBuilder.Instance?.Select(ModuleKind.Water)));
            build.Add(Button("Tower", () => GridBuilder.Instance?.Select(ModuleKind.Watchtower)));
            build.Add(Button("Generator", () => GridBuilder.Instance?.Select(ModuleKind.Generator)));
            build.Add(Button("Bench", () => GridBuilder.Instance?.Select(ModuleKind.Workbench)));
            build.Add(Button("Post", () => GridBuilder.Instance?.Select(ModuleKind.TradingPost)));
            camp.Add(build);
            camp.Add(Body("Craft"));
            bool bench = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Workbench");
            foreach (var recipe in CraftingBench.Recipes)
            {
                string id = recipe.Id;
                int cost = CraftingBench.Priced(recipe.ScrapCost, bench);
                camp.Add(Button(recipe.Label + " (" + cost + ")", () => CraftingBench.Instance?.Craft(id)));
            }
            var map = WorldMapService.Instance;
            if (map != null)
            {
                camp.Add(Body("Radio " + CampaignBoard.PartCount(map.Parts) + "/3  " + DifficultyProfile.Name(map.Difficulty) + "  seed " + map.WorldSeed));
                camp.Add(Button("Reroll street", () => map.RerollSeed()));
                if (map.Endless) camp.Add(Body("The broadcast holds. The nights keep coming."));
                else if (map.CampaignWon) camp.Add(Body("The tower is on the air."));
                else if (map.ReadyToBroadcast) camp.Add(Button("Broadcast night", () => NightRaidController.Instance?.BeginBroadcast()));
                else camp.Add(Body("The tower needs three radio parts and a built generator."));
                camp.Add(Body("District"));
                foreach (var district in map.Districts)
                {
                    if (district.cleared && !map.Endless)
                    {
                        string part = CampaignBoard.PartFor(district.id);
                        camp.Add(Body(district.displayName + " — clear" + (string.IsNullOrEmpty(part) ? "" : "  part")));
                        continue;
                    }
                    string id = district.id;
                    if (!CampaignBoard.Reachable(id, ClearedDistricts(map)))
                    {
                        camp.Add(Body(district.displayName + " — road closed"));
                        continue;
                    }
                    string mark = map.Current != null && map.Current.id == id ? "> " : "";
                    string hours = CampaignBoard.TravelHours(id).ToString("0");
                    camp.Add(Button(mark + district.displayName + "  " + hours + "h", () => map.Select(id)));
                }
            }
            camp.Add(Button("Leave for the district", () => Go(FlowStep.Expedition, () => GameManager.Instance.BeginExpedition())));
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
            var track = new VisualElement();
            track.style.height = 8;
            track.style.marginBottom = 8;
            track.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            var fill = new VisualElement();
            float ratio = Mathf.Clamp01(inventory.WeightRatio);
            fill.style.width = Length.Percent(ratio * 100f);
            fill.style.height = 8;
            bool heavy = PackOps.Heavy(inventory.CurrentWeight, inventory.MaxWeightCapacity);
            fill.style.backgroundColor = heavy ? new Color(0.75f, 0.2f, 0.16f) : new Color(0.35f, 0.62f, 0.38f);
            track.Add(fill);
            pack.Add(track);
            if (inventory.MedicalKits > 0)
            {
                var medRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var medLabel = Body();
                medLabel.text = "Medkit x" + inventory.MedicalKits;
                medLabel.style.flexGrow = 1;
                medRow.Add(medLabel);
                medRow.Add(Button("Use", () => inventory.UseMedkit()));
                string medMark = inventory.BeltMark("medkit");
                medRow.Add(Button(string.IsNullOrEmpty(medMark) ? "Belt" : "Belt " + medMark, () => inventory.ToggleBelt("medkit")));
                pack.Add(medRow);
            }
            if (inventory.ScrapCount > 0) pack.Add(Body("Scrap x" + inventory.ScrapCount));
            var scroll = new ScrollView();
            scroll.style.height = 220;
            foreach (var item in inventory.Items)
            {
                string id = item.ItemId;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var label = Body();
                label.text = item.ItemName + " x" + item.Quantity;
                label.style.flexGrow = 1;
                row.Add(label);
                row.Add(Button("Use", () => inventory.TryUse(id)));
                row.Add(Button("Drop", () => inventory.Drop(id, 1)));
                row.Add(Button("Split", () => inventory.DropHalf(id)));
                if (ItemBelt.Fits(id))
                {
                    string mark = inventory.BeltMark(id);
                    row.Add(Button(string.IsNullOrEmpty(mark) ? "Belt" : "Belt " + mark, () => inventory.ToggleBelt(id)));
                }
                scroll.Add(row);
            }
            pack.Add(scroll);
            var crate = LootContainer.Open;
            if (crate != null && crate.HeldCount > 0)
            {
                pack.Add(Body("Container"));
                for (int i = 0; i < crate.HeldCount; i++)
                {
                    string id = crate.HeldId(i);
                    pack.Add(Button("Take " + crate.HeldOffer(i), () => crate.Take(id, inventory)));
                }
                pack.Add(Button("Take all", () => crate.TakeAll(inventory)));
            }
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

        private void DrawSlots(VisualElement menu)
        {
            menu.Add(Title("SAVES"));
            var cards = SaveSystem.Instance != null ? SaveSystem.Instance.Cards() : System.Array.Empty<SaveSlots.Card>();
            for (int i = 0; i < SaveSlots.ManualCount; i++)
            {
                SaveSlots.Card card = default;
                for (int c = 0; c < cards.Length; c++)
                {
                    if (cards[c].Slot == i) card = cards[c];
                }
                int index = i;
                string label = card.Occupied
                    ? "Slot " + (i + 1) + "  day " + card.Day + "  " + card.Leader
                    : "Slot " + (i + 1) + "  empty";
                menu.Add(Button(label, () =>
                {
                    slotsOpen = false;
                    if (card.Occupied)
                    {
                        Go(FlowStep.Sanctuary, () =>
                        {
                            if (SaveSystem.Instance == null || !SaveSystem.Instance.LoadSlot(index))
                                GameManager.Instance.SetState(GameState.MainMenu);
                        });
                    }
                    else
                    {
                        SaveSystem.Instance?.UseSlot(index);
                        Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost());
                    }
                }));
            }
            SaveSlots.Card auto = default;
            for (int c = 0; c < cards.Length; c++)
            {
                if (cards[c].Slot == SaveSlots.AutoSlot) auto = cards[c];
            }
            if (auto.Occupied)
            {
                menu.Add(Button("Autosave  day " + auto.Day + "  " + auto.Leader, () =>
                {
                    slotsOpen = false;
                    Go(FlowStep.Sanctuary, () =>
                    {
                        if (SaveSystem.Instance == null || !SaveSystem.Instance.LoadSlot(SaveSlots.AutoSlot))
                            GameManager.Instance.SetState(GameState.MainMenu);
                    });
                }));
            }
            menu.Add(Button("Back", () => slotsOpen = false));
        }

        private static string[] ClearedDistricts(WorldMapService map)
        {
            int count = 0;
            foreach (var district in map.Districts) if (district.cleared) count++;
            var ids = new string[count];
            int write = 0;
            foreach (var district in map.Districts)
            {
                if (!district.cleared) continue;
                ids[write] = district.id;
                write++;
            }
            return ids;
        }

        private static string CampSignature()
        {
            var builder = new StringBuilder();
            if (WorldClock.Instance != null) builder.Append(WorldClock.Instance.Day);
            if (ColonyStorage.Instance != null) builder.Append(ColonyStorage.Instance.Scrap).Append(ColonyStorage.Instance.Food).Append(ColonyStorage.Instance.Security);
            if (SurvivorRoster.Instance != null)
            {
                foreach (var survivor in SurvivorRoster.Instance.Survivors)
                {
                    builder.Append(survivor.id).Append(survivor.task).Append(survivor.alive).Append(survivor.leader);
                    builder.Append(Mathf.RoundToInt(survivor.morale)).Append(Mathf.RoundToInt(survivor.hunger)).Append(survivor.opinion).Append(survivor.injury);
                }
                builder.Append(SurvivorRoster.Instance.DayNotes);
                builder.Append(SurvivorRoster.Instance.PackMemorials());
            }
            if (GridBuilder.Instance != null)
            {
                builder.Append(GridBuilder.Instance.Selected);
                foreach (var module in GridBuilder.Instance.Placed) builder.Append(module.kind);
            }
            if (CampServices.Instance != null) builder.Append(CampServices.Instance.GeneratorOnline);
            if (WorldMapService.Instance != null && WorldMapService.Instance.Current != null)
            {
                builder.Append(WorldMapService.Instance.Current.id);
                builder.Append(WorldMapService.Instance.Parts);
                builder.Append(WorldMapService.Instance.BroadcastWon);
                builder.Append(WorldMapService.Instance.ClearedCount);
                builder.Append(WorldMapService.Instance.WorldSeed);
            }
            if (FactionTrade.Instance != null) builder.Append(FactionTrade.Instance.Signature);
            return builder.ToString();
        }

        private static string PackSignature()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (inventory == null) return "";
            var builder = new StringBuilder();
            foreach (var item in inventory.Items) builder.Append(item.ItemId).Append(item.Quantity);
            builder.Append(inventory.MedicalKits);
            builder.Append(inventory.ScrapCount);
            builder.Append(Mathf.RoundToInt(inventory.CurrentWeight * 10f));
            if (LootContainer.Open != null) builder.Append(LootContainer.Open.Contents);
            builder.Append(inventory.BeltLine);
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
            var button = new Button(() =>
            {
                AudioManager.Instance?.Play("ui", 0.4f);
                action?.Invoke();
            }) { text = text };
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

using System.Collections;
using System.Collections.Generic;
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
    /// Runtime UI Toolkit surface for the pack, camp board, and menus; the street HUD is <see cref="HudController"/>.
    /// Refreshes on unscaled time so pause, succession, and the main menu stay interactive. During play with no
    /// panel open the refresh compares cached or hashed keys, so it allocates nothing.
    /// </summary>
    public class OutpostInterface : MonoBehaviour
    {
        private UIDocument document;
        private VisualElement root;
        private VisualElement menu;
        private VisualElement camp;
        private readonly List<VisualElement> campLit = new List<VisualElement>();
        private VisualElement guideBox;
        private string campMark = "";
        private VisualElement pack;
        private string menuKey = "";
        private int campKey = int.MinValue + 1;
        private int packKey = int.MinValue + 1;
        private GameShellUI shell;
        private int listening = -1;
        private int padListen = -1;
        private bool askKeep;
        private string bindNote = "";
        private bool settingsWasOpen;
        private readonly ScreenStack screens = new ScreenStack();
        private GameState screensState = GameState.MainMenu;
        private string seedText = "";
        private string codexId = "";
        private bool skipTutorial;
        private string inspected = "";
        private int packFilter = PackFilter.All;
        private BuildMenu.Tab buildTab = BuildMenu.Tab.Defence;

        private void Update()
        {
            InputGlyphs.Poll();
            if (ExpeditionInput.WatchPressed) AiWatch.Toggle();
            if (listening < 0 && padListen < 0 && ExpeditionInput.PausePressed) Back();

            if (padListen >= 0)
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    padListen = -1;
                    menuKey = "";
                    return;
                }
                for (int i = 0; i < PadBindings.Buttons.Length; i++)
                {
                    if (!ExpeditionInput.PadButtonPressed(PadBindings.Buttons[i])) continue;
                    if (PadBindings.TryRebindNamed((PadBindings.Action)padListen, PadBindings.Buttons[i]))
                        SettingsService.Instance?.NoteBindings();
                    padListen = -1;
                    menuKey = "";
                    return;
                }
            }

            if (listening < 0 || Keyboard.current == null) return;
            foreach (Key key in (Key[])System.Enum.GetValues(typeof(Key)))
            {
                if (key == Key.None) continue;
                var control = Keyboard.current[key];
                if (control == null || !control.wasPressedThisFrame) continue;
                if (key != Key.Escape)
                {
                    var action = (ControlBindings.Action)listening;
                    bindNote = BindNote.For(ControlBindings.Check(action, key), key, ControlBindings.Holder(action, key), null);
                    if (ControlBindings.TryRebind(action, key)) SettingsService.Instance?.NoteBindings();
                }
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
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 1f;
            panel.sortingOrder = 20;
            document = Attach.Ensure<UIDocument>(gameObject);
            document.panelSettings = panel;
            PanelScale.Track(panel);
            root = document.rootVisualElement;
            if (root == null) return;
            FontFallback.Dress(root);

            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Position;

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
            PanelScale.Apply(SettingsService.Instance != null ? SettingsService.Instance.UiScale : 1f);
            FontFallback.Apply(SettingsService.Instance != null ? SettingsService.Instance.Language : "en");
            root.style.fontSize = Mathf.RoundToInt(14 * scale);
            if (shell == null) shell = FindFirstObjectByType<GameShellUI>();
            bool inventory = shell != null && shell.InventoryOpen;
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            if (state != screensState)
            {
                screensState = state;
                screens.Clear();
                codexId = "";
            }
            bool settings = SettingsService.Instance != null && SettingsService.Instance.ShowSettings;
            if (settings && !settingsWasOpen) SettingsService.Instance.BeginEdit();
            if (!settings) askKeep = false;
            settingsWasOpen = settings;
            bool trade = FactionTrade.Instance != null && FactionTrade.Instance.Open;
            string nextMenu;
            if (!settings && !trade && MenuQuiet(state)) nextMenu = QuietKey(state);
            else
            {
                string language = SettingsService.Instance != null ? SettingsService.Instance.Language : "en";
                string discrete = SettingsService.Instance != null ? SettingsService.Instance.DiscreteKey : "";
                string tradeKey = FactionTrade.Instance != null ? FactionTrade.Instance.Signature : "";
                nextMenu = state + "|" + settings + "|" + trade + "|" + language + "|" + discrete + "|" + ControlBindings.Signature() + "|" + PadBindings.Signature() + "|" + listening + "|" + padListen + "|" + screens.Signature() + "|" + NewGameSignature() + "|" + codexId + "|" + tradeKey + "|" + askKeep + "|" + bindNote + "|" + InputGlyphs.UsingPad;
            }
            if (nextMenu != menuKey)
            {
                menuKey = nextMenu;
                RebuildMenu(state, settings, trade);
            }

            int nextCamp = state == GameState.CampManagement ? CampKey() : Closed;
            if (nextCamp != campKey)
            {
                campKey = nextCamp;
                RebuildCamp(state == GameState.CampManagement);
            }

            int nextPack = inventory ? PackKey() : Closed;
            if (nextPack != packKey)
            {
                packKey = nextPack;
                RebuildPack(inventory);
            }

        }

        /// <summary>States whose menu panel is empty unless settings or a trade stall is open.</summary>
        public static bool MenuQuiet(GameState state)
        {
            return state == GameState.ExpeditionActive || state == GameState.RaidActive || state == GameState.CampManagement;
        }

        private static readonly string[] quietKeys = new string[16];

        private static string QuietKey(GameState state)
        {
            int slot = (int)state & 15;
            return quietKeys[slot] ?? (quietKeys[slot] = "quiet|" + state);
        }

        private void Back()
        {
            var gm = GameManager.Instance;
            var state = gm != null ? gm.CurrentState : GameState.ExpeditionActive;
            bool settings = SettingsService.Instance != null && SettingsService.Instance.ShowSettings;
            bool trade = FactionTrade.Instance != null && FactionTrade.Instance.Open;
            bool pausable = gm != null && (state == GameState.Paused || state == GameState.ExpeditionActive || state == GameState.RaidActive || state == GameState.CampManagement);
            switch (BackRoute.For(settings, trade, screens.Depth, pausable))
            {
                case BackAction.CloseSettings:
                    CloseSettings();
                    break;
                case BackAction.CloseTrade:
                    FactionTrade.Instance.Toggle();
                    break;
                case BackAction.Pop:
                    if (screens.Pop() == MenuScreen.CodexEntry) codexId = "";
                    break;
                case BackAction.TogglePause:
                    gm.TogglePause();
                    break;
            }
        }

        private void CloseSettings()
        {
            var settings = SettingsService.Instance;
            if (settings == null) return;
            switch (SettingsDraft.OnClose(settings.HasUnsaved, askKeep))
            {
                case SettingsClose.Ask:
                    askKeep = true;
                    break;
                case SettingsClose.StayOpen:
                    askKeep = false;
                    break;
                default:
                    FinishSettings(false);
                    break;
            }
        }

        private void FinishSettings(bool revert)
        {
            var settings = SettingsService.Instance;
            if (settings == null) return;
            if (revert) settings.RevertEdits();
            else settings.KeepEdits();
            askKeep = false;
            bindNote = "";
            listening = -1;
            padListen = -1;
            if (settings.ShowSettings) settings.TogglePanel();
        }

        private void Open(MenuScreen screen) => screens.Push(screen);

        private void Close()
        {
            if (screens.Pop() == MenuScreen.CodexEntry) codexId = "";
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
                bool shown = selected != null && CodexBook.Visible(selected, packed);
                var portrait = shown ? CodexIcons.For(selected) : null;
                if (portrait != null) parent.Add(Picture(portrait, 96));
                parent.Add(Title(shown ? Loc.EntryTitle(selected.Id, selected.Title) : Loc.T("camp.unknown")));
                parent.Add(Body(shown ? Loc.EntryBody(selected.Id, selected.Body) : Loc.T("camp.unseen")));
                parent.Add(Button(Loc.T("menu.back"), Close));
                return;
            }
            parent.Add(Title(Loc.T("menu.codex")));
            for (int i = 0; i < CodexBook.Entries.Length; i++)
            {
                var entry = CodexBook.Entries[i];
                string id = entry.Id;
                bool visible = CodexBook.Visible(entry, packed);
                var line = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var thumb = visible ? CodexIcons.For(entry) : null;
                if (thumb != null) line.Add(Picture(thumb, 28));
                line.Add(Button(visible ? Loc.EntryTitle(entry.Id, entry.Title) : Loc.T("camp.unknown"), () => { codexId = id; Open(MenuScreen.CodexEntry); }));
                parent.Add(line);
            }
            parent.Add(Button(Loc.T("codex.replay"), () => CodexDirector.Instance?.ReplayHints()));
            parent.Add(Button(Loc.T("set.close"), Close));
        }

        private static void Go(FlowStep step, System.Action arrived = null)
        {
            if (SceneFlow.Instance != null)
            {
                SceneFlow.Instance.Travel(step, arrived);
                return;
            }
            if (arrived != null) arrived();
            else GameManager.Instance?.Arrive(new FlowContext(step, step, false));
        }

        private static void DrawTrade(VisualElement parent, FactionTrade faction)
        {
            string id = faction.ActiveId;
            int standing = faction.StandingOf(id);
            string name = StallVoice.Name(id, null);
            parent.Add(Title(name + "  " + standing));
            if (CaravanBook.Refuses(id, standing))
            {
                parent.Add(Body(StallVoice.Refuse(name, null)));
            }
            else
            {
                string[] stock = faction.Stock;
                for (int i = 0; i < stock.Length; i++)
                {
                    string itemId = stock[i];
                    string label = Loc.Item(itemId);
                    if (itemId == CaravanBook.Premium(id)) label += "  " + Loc.T("stall.trusted");
                    parent.Add(Button(StallVoice.Buy(label, faction.Price(itemId), null), () => faction.Buy(itemId)));
                }
                if (standing < CaravanBook.Trusted) parent.Add(Body(Loc.T("stall.trust_at") + " " + CaravanBook.Trusted));
                var pack = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
                if (pack != null)
                {
                    foreach (var carried in pack.Items)
                    {
                        if (carried == null || carried.Quantity <= 0 || !CaravanBook.Sellable(carried.ItemId)) continue;
                        string sold = carried.ItemId;
                        parent.Add(Button(Loc.T("stall.sell_item") + " " + Loc.Item(sold) + " x" + carried.Quantity + "  +" + faction.Offer(sold), () => faction.Sell(sold)));
                    }
                }
            }
            string questId = id == "clinic" || id == "farmers" ? id : "caravan";
            parent.Add(Body(StallVoice.Quest(id, CaravanBook.QuestDone(faction.Quests, questId), null)));
            if (id == "clinic" && !CaravanBook.QuestDone(faction.Quests, "clinic"))
            {
                parent.Add(Button(Loc.T("stall.deliver"), () => faction.DeliverMedkits()));
            }
            parent.Add(Button(Loc.T("stall.leave"), faction.Toggle));
        }

        private static string Marks(Survivor survivor)
        {
            if (survivor == null) return "";
            string marks = Loc.Trait(survivor.trait);
            if (!string.IsNullOrEmpty(survivor.aside)) marks += " · " + Loc.Trait(survivor.aside);
            if (!string.IsNullOrEmpty(survivor.mark)) marks += " · " + Loc.Trait(survivor.mark);
            return marks;
        }

        private static string Bonds(string kin)
        {
            string text = "";
            string closeId = KinBoard.Closest(kin);
            if (closeId.Length > 0)
            {
                string kind = BondMark.Kind(KinBoard.Read(kin, closeId));
                string key = kind == "Partner" ? "bond.partner" : "bond.friend";
                text += "  " + Loc.T(key) + " " + closeId;
            }
            string bitter = KinBoard.Bitter(kin);
            if (bitter.Length > 0) text += "  " + Loc.T("bond.rival") + " " + bitter;
            return text;
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
                    if (screens.Top == MenuScreen.Codex || screens.Top == MenuScreen.CodexEntry)
                    {
                        DrawCodex(menu);
                        break;
                    }
                    menu.Add(Title(Loc.T("menu.pause")));
                    menu.Add(Button(Loc.T("menu.resume"), () => GameManager.Instance.TogglePause()));
                    menu.Add(Button(Loc.T("menu.settings"), () => SettingsService.Instance?.TogglePanel()));
                    menu.Add(Button(Loc.T("menu.save"), () => SaveSystem.Instance?.Save()));
                    menu.Add(Button(Loc.T("menu.codex"), () => { codexId = ""; Open(MenuScreen.Codex); }));
                    menu.Add(Button(Loc.T("menu.skip"), () => TutorialDirector.Instance?.Dismiss()));
                    menu.Add(Button(Loc.T("menu.camp"), () => Go(FlowStep.Sanctuary)));
                    menu.Add(Button(Loc.T("menu.restart"), () => Go(FlowStep.Boot, () => GameManager.Instance.ReturnToBoot())));
                    menu.Add(Button(Loc.T("menu.save_quit"), () =>
                    {
                        SaveSystem.Instance?.Save();
                        Go(FlowStep.MainMenu);
                    }));
                    menu.Add(Button(Loc.T("menu.quit"), () =>
                    {
                        SaveSystem.Instance?.Save();
                        Quit();
                    }));
                    break;
                case GameState.SuccessionScreen:
                    menu.Add(Title(Loc.T("menu.leader")));
                    if (SurvivorRoster.Instance != null && SurvivorRoster.Instance.Memorials.Count > 0)
                    {
                        var fallen = SurvivorRoster.Instance.Memorials[SurvivorRoster.Instance.Memorials.Count - 1];
                        menu.Add(Body(SuccessionLedger.Card(fallen)));
                    }
                    menu.Add(Body(Loc.T("menu.choose")));
                    if (SurvivorRoster.Instance != null)
                    {
                        var heir = SurvivorRoster.Instance.SuggestedHeir();
                        foreach (var survivor in SurvivorRoster.Instance.Survivors)
                        {
                            if (!survivor.alive) continue;
                            string id = survivor.id;
                            bool suggested = heir != null && heir.id == id;
                            var pick = Button((suggested ? "> " : "") + survivor.displayName + " — " + Marks(survivor) + "  " + Loc.Mood(ColonyDay.Mood(survivor.morale, survivor.trait, survivor.aside, survivor.mark)) + (suggested ? "  " + Loc.T("menu.heir") : ""), () => GameManager.Instance.AcceptSuccessor(id));
                            if (suggested) pick.style.backgroundColor = new Color(0.32f, 0.26f, 0.14f);
                            menu.Add(pick);
                        }
                    }
                    menu.Add(Button(Loc.T("menu.falls"), () => GameManager.Instance.SetState(GameState.GameOver)));
                    break;
                case GameState.Victory:
                    menu.Add(Title(Loc.T("menu.holds")));
                    menu.Add(Body(Loc.T("menu.broadcast")));
                    DrawHaul(menu);
                    DrawBoard(menu);
                    menu.Add(Button(Loc.T("menu.enter"), () => Go(FlowStep.Sanctuary)));
                    menu.Add(Button(Loc.T("menu.endless"), () => Go(FlowStep.Sanctuary, () =>
                    {
                        WorldMapService.Instance?.TryBeginEndless();
                        GameManager.Instance.EnterCamp();
                    })));
                    menu.Add(Button(Loc.T("menu.new"), () => Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost())));
                    break;
                case GameState.ExpeditionResults:
                    menu.Add(Title(Loc.T("result.title")));
                    var map = WorldMapService.Instance;
                    menu.Add(Body(map != null && map.CampaignWon ? Loc.T("menu.air") : Loc.T("menu.supplies")));
                    DrawHaul(menu);
                    if (map != null && map.Current != null) menu.Add(Body(Loc.T("menu.next") + " " + Loc.District(map.Current.id)));
                    menu.Add(Button(Loc.T("menu.enter"), () => Go(FlowStep.Sanctuary)));
                    break;
                case GameState.GameOver:
                    menu.Add(Title(Loc.T("gameover.title")));
                    int remembered = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Memorials.Count : 0;
                    menu.Add(Body(remembered > 0 ? remembered + " " + Loc.T("menu.names") : Loc.T("menu.names_none")));
                    DrawBoard(menu);
                    menu.Add(Button(Loc.T("menu.new"), () => Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost())));
                    break;
                case GameState.MainMenu:
                    if (screens.Top == MenuScreen.Credits)
                    {
                        menu.Add(Title(Loc.T("menu.credits")));
                        menu.Add(Body(Loc.T("menu.brand") + " " + BuildStamp.Version));
                        menu.Add(Body(Loc.T("menu.blurb")));
                        menu.Add(Body(Loc.T("menu.tones")));
                        menu.Add(Body(SoundCredit.Count + " " + Loc.T("menu.tones_n")));
                        menu.Add(Button(Loc.T("menu.back"), Close));
                        break;
                    }
                    if (screens.Top == MenuScreen.Saves)
                    {
                        DrawSlots(menu);
                        break;
                    }
                    if (screens.Top == MenuScreen.NewGame)
                    {
                        DrawNewGame(menu);
                        break;
                    }
                    menu.Add(Title(Loc.T("menu.title")));
                    menu.Add(Body(Loc.T("menu.version") + " " + BuildStamp.Version));
                    menu.Add(Button(Loc.T("menu.continue"), () => Go(FlowStep.Sanctuary, () =>
                    {
                        if (SaveSystem.Instance == null || !SaveSystem.Instance.Load())
                            GameManager.Instance.SetState(GameState.MainMenu);
                    })));
                    menu.Add(Button(Loc.T("menu.saves"), () => Open(MenuScreen.Saves)));
                    menu.Add(Button(Loc.T("menu.new"), () => Open(MenuScreen.NewGame)));
                    menu.Add(Button(Loc.T("menu.settings"), () => SettingsService.Instance?.TogglePanel()));
                    menu.Add(Button(Loc.T("menu.credits"), () => Open(MenuScreen.Credits)));
                    menu.Add(Button(Loc.T("menu.street"), () => Go(FlowStep.Expedition)));
                    menu.Add(Button(Loc.T("menu.quit"), Quit));
                    break;
            }
            menu.style.display = menu.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private string NewGameSignature()
        {
            var settings = SettingsService.Instance;
            return (settings != null ? settings.NextDifficulty + ":" + settings.Merciful : "") + ":" + skipTutorial;
        }

        private void DrawNewGame(VisualElement menu)
        {
            var settings = SettingsService.Instance;
            menu.Add(Title(Loc.T("new.title")));
            menu.Add(Button(Loc.T("set.next") + " " + Loc.Difficulty(settings != null ? settings.NextDifficulty : 2), () => settings?.CycleDifficulty()));
            if (settings != null) menu.Add(Button(settings.Merciful ? Loc.T("set.merciful") : Loc.T("set.perma"), settings.ToggleMerciful));
            menu.Add(Button(skipTutorial ? Loc.T("new.tut_off") : Loc.T("new.tut_on"), () => skipTutorial = !skipTutorial));
            menu.Add(Body(Loc.T("new.seed")));
            var seed = new TextField { value = seedText, maxLength = NewGamePlan.MaxLength };
            seed.style.width = 260;
            seed.style.marginBottom = 6;
            seed.RegisterValueChangedCallback(evt => seedText = evt.newValue ?? "");
            menu.Add(seed);
            menu.Add(Button(Loc.T("new.start"), () =>
            {
                screens.Clear();
                string chosen = seedText;
                bool skip = skipTutorial;
                Go(FlowStep.Sanctuary, () => GameManager.Instance.BeginNewOutpost(chosen, skip));
            }));
            menu.Add(Button(Loc.T("menu.back"), Close));
        }

        private void DrawHaul(VisualElement menu)
        {
            var gm = GameManager.Instance;
            var tracker = ObjectiveTracker.Instance;
            var outcome = gm != null ? gm.LastOutcome : default;
            bool closed = outcome.end != ExpeditionEnd.None;
            string district = ExtractSlip.Place(gm != null ? gm.LastStreet : "", null);
            int kills = closed ? outcome.kills : gm != null ? gm.ZombiesKilled : 0;
            int scrap = closed ? outcome.scrap : gm != null ? gm.ScrapLooted : 0;
            int killGoal = closed ? outcome.killGoal : tracker != null ? tracker.KillGoal : 1;
            int scrapGoal = closed ? outcome.scrapGoal : tracker != null ? tracker.ScrapGoal : 1;
            menu.Add(Body(ExtractSlip.Line(district, kills, killGoal, scrap, scrapGoal, Loc.T("result.kills"), Loc.T("result.scrap"))));
            if (closed) menu.Add(Body(ExpeditionLedger.TimeLine(outcome, null)));
            if (tracker != null && tracker.PoiLine().Length > 0) menu.Add(Body(tracker.PoiLine()));
        }

        private void DrawBoard(VisualElement menu)
        {
            menu.Add(Body(Loc.T("menu.board")));
            var runs = RunArchive.Instance != null ? RunArchive.Instance.Runs : null;
            if (runs == null || runs.Length == 0)
            {
                menu.Add(Body(Loc.T("menu.board_empty")));
                return;
            }
            int count = runs.Length < 4 ? runs.Length : 4;
            for (int i = 0; i < count; i++) menu.Add(Body(RunBoard.Line(runs[i], null)));
        }

        private void BuildSettings(VisualElement parent)
        {
            var settings = SettingsService.Instance;
            parent.Add(Title(Loc.T("set.title")));
            if (askKeep)
            {
                parent.Add(Body(Loc.T("set.unsaved")));
                parent.Add(Button(Loc.T("set.keep"), () => FinishSettings(false)));
                parent.Add(Button(Loc.T("set.revert"), () => FinishSettings(true)));
                parent.Add(Button(Loc.T("set.back"), () => askKeep = false));
                parent.style.display = DisplayStyle.Flex;
                return;
            }
            parent.Add(SliderRow(Loc.T("set.shake"), settings.ScreenShake, settings.SetShake));
            parent.Add(SliderRow(Loc.T("set.volume"), settings.MasterVolume, settings.SetVolume));
            parent.Add(SliderRow(Loc.T("set.effects"), settings.SfxVolume, settings.SetSfx));
            parent.Add(SliderRow(Loc.T("set.music"), settings.MusicVolume, settings.SetMusic));
            parent.Add(SliderRow(Loc.T("set.ambience"), settings.AmbienceVolume, settings.SetAmbience));
            parent.Add(SliderRow(Loc.T("set.ui"), settings.UiVolume, settings.SetUi));
            parent.Add(SliderRow(Loc.T("set.fov"), settings.FieldOfView, 40f, 75f, settings.SetFieldOfView));
            parent.Add(SliderRow(Loc.T("set.text"), settings.TextScale, 0.8f, 1.6f, settings.SetTextScale));
            parent.Add(SliderRow(Loc.T("set.ui_scale"), settings.UiScale, PlayOptions.UiScaleMin, PlayOptions.UiScaleMax, settings.SetUiScale));
            parent.Add(SliderRow(Loc.T("set.hud"), settings.HudOpacity, 0.45f, 1f, settings.SetHudOpacity));
            parent.Add(SliderRow(Loc.T("set.bright"), settings.Brightness, 0.6f, 1.4f, settings.SetBrightness));
            parent.Add(Button(settings.Subtitles ? Loc.T("set.subs_on") : Loc.T("set.subs_off"), () => settings.SetSubtitles(!settings.Subtitles)));
            parent.Add(Button(settings.QuietFlash ? Loc.T("set.flash_off") : Loc.T("set.flash_on"), settings.ToggleQuietFlash));
            parent.Add(Button(Loc.T("set.color") + " " + Loc.T(HudPalette.Name(settings.ColorblindMode)), settings.CycleColorblind));
            parent.Add(Button(settings.EnemyOutline ? Loc.T("set.outline_on") : Loc.T("set.outline_off"), settings.ToggleEnemyOutline));
            bool devTongue = DevCheats.Allowed(Application.isEditor, Debug.isDebugBuild);
            parent.Add(Button(PseudoLoc.Label(settings.Language), () => settings.SetLanguage(PseudoLoc.Next(settings.Language, devTongue))));
            int tier = Mathf.Clamp(settings.Quality, 0, 3);
            parent.Add(Button(Loc.T("set.quality") + " " + Loc.T("set.tier" + tier), settings.CycleQuality));
            parent.Add(Button(settings.VSync ? Loc.T("set.vsync_on") : Loc.T("set.vsync_off"), settings.ToggleVSync));
            parent.Add(Button(Loc.T("set.frame") + " " + PlayOptions.FrameName(settings.FrameCap, null), settings.CycleFrameCap));
            parent.Add(Button(Loc.T("set.resolution") + " " + DisplayModes.Name(settings.Resolution), settings.CycleResolution));
            parent.Add(Button(Loc.T("set.render") + " " + PlayOptions.ScaleName(settings.RenderScaleStep, null), settings.CycleRenderScale));
            parent.Add(Button(settings.AimAssist == 0 ? Loc.T("set.aim_off") : settings.AimAssist == 2 ? Loc.T("set.aim_strong") : Loc.T("set.aim_light"), settings.CycleAim));
            parent.Add(Button(settings.InvertLook ? Loc.T("set.invert") : Loc.T("set.look"), settings.ToggleInvert));
            parent.Add(Button(settings.CrouchMode == 1 ? Loc.T("set.crouch_toggle") : Loc.T("set.crouch_hold"), settings.ToggleCrouchMode));
            parent.Add(Button(settings.SprintMode == 1 ? Loc.T("set.sprint_toggle") : Loc.T("set.sprint_hold"), settings.ToggleSprintMode));
            parent.Add(Button(settings.Merciful ? Loc.T("set.merciful") : Loc.T("set.perma"), settings.ToggleMerciful));
            parent.Add(Button(Loc.T("set.next") + " " + Loc.Difficulty(settings.NextDifficulty), settings.CycleDifficulty));
            parent.Add(Button(Loc.T("set.gore") + " " + Presentation.GoreName(settings.Gore == 0 ? 3 : settings.Gore), settings.CycleGore));
            parent.Add(Button(settings.HitStop ? Loc.T("set.hit_on") : Loc.T("set.hit_off"), settings.ToggleHitStop));
            parent.Add(Button(settings.DamageNumbers ? Loc.T("set.num_on") : Loc.T("set.num_off"), settings.ToggleDamageNumbers));
            parent.Add(Button(settings.MotionBlur ? Loc.T("set.blur_on") : Loc.T("set.blur_off"), settings.ToggleMotionBlur));
            parent.Add(Button(settings.WindowMode == 1 ? Loc.T("set.window") : settings.WindowMode == 2 ? Loc.T("set.full") : Loc.T("set.display"), settings.CycleWindow));
            parent.Add(Body(Loc.T("set.keys")));
            if (bindNote.Length > 0) parent.Add(Body(bindNote));
            for (int i = 0; i < ControlBindings.Count; i++)
            {
                var action = (ControlBindings.Action)i;
                int index = i;
                string caption = listening == index
                    ? MenuLine.KeyWait(action.ToString(), null)
                    : MenuLine.KeyBound(action.ToString(), ControlBindings.Label(action), null);
                parent.Add(Button(caption, () =>
                {
                    listening = index;
                    padListen = -1;
                }));
            }
            parent.Add(Button(Loc.T("set.reset_keys"), () =>
            {
                ControlBindings.ResetDefaults();
                listening = -1;
                SettingsService.Instance?.NoteBindings();
                menuKey = "";
            }));
            parent.Add(Body(Loc.T("set.pad")));
            for (int i = 0; i < PadBindings.Count; i++)
            {
                var action = (PadBindings.Action)i;
                int index = i;
                string caption = padListen == index
                    ? MenuLine.PadWait(action.ToString(), null)
                    : MenuLine.PadBound(action.ToString(), PadBindings.Label(action), null);
                parent.Add(Button(caption, () =>
                {
                    padListen = index;
                    listening = -1;
                }));
            }
            parent.Add(Button(Loc.T("set.reset_pad"), () =>
            {
                PadBindings.ResetDefaults();
                padListen = -1;
                SettingsService.Instance?.NoteBindings();
                menuKey = "";
            }));
            parent.Add(Button(Loc.T("set.revert"), () =>
            {
                settings.RevertEdits();
                settings.BeginEdit();
                listening = -1;
                padListen = -1;
                bindNote = "";
                menuKey = "";
            }));
            parent.Add(Button(Loc.T("set.close"), CloseSettings));
            parent.style.display = DisplayStyle.Flex;
        }

        private void RebuildCamp(bool open)
        {
            camp.Clear();
            camp.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open) return;
            camp.Add(Title(Loc.T("camp.title")));
            var guide = TutorialDirector.Instance;
            campMark = guide != null ? guide.CampMark : "";
            campLit.Clear();
            guideBox = null;
            if (guide != null && !guide.CampFinished && guide.CampCurrent.Length > 0)
            {
                guideBox = new VisualElement();
                guideBox.Add(Body(Loc.T("tut.day1") + " " + (guide.CampIndex + 1) + "/" + TutorialTrack.Camp.Length + ": " + guide.CampCurrent));
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                if (guide.CampAwaitsRead) row.Add(Button(Loc.T("tut.next"), () => guide.Note(TutorialTrack.Read)));
                row.Add(Button(Loc.T("menu.skip"), guide.Dismiss));
                guideBox.Add(row);
                camp.Add(guideBox);
            }
            var storage = ColonyStorage.Instance;
            var services = CampServices.Instance;
            if (storage != null)
            {
                camp.Add(Lit(Body(Loc.T("camp.scrap") + " " + storage.Scrap + "  " + Loc.T("camp.food") + " " + storage.Food + "  " + Loc.T("camp.water") + " " + storage.Water
                    + "  " + Loc.T("camp.cloth") + " " + storage.Cloth + "  " + Loc.T("camp.chem") + " " + storage.Chemicals                     + "  " + Loc.T("camp.tape") + " " + storage.Tape + "  " + Loc.T("camp.raw") + " " + storage.Raw + "  " + Loc.T("camp.rounds") + " " + storage.Rounds
                    + "  " + (services != null && services.GeneratorOnline ? Loc.T("camp.gen_on") : Loc.T("camp.gen_off"))
                    + (services != null ? "  " + Loc.T("camp.fuel") + " " + FuelTank.Label(services.FuelHours) + StormNote(services) : "")), TutorialMark.Stores));
                camp.Add(Body(Loc.T("camp.room") + " " + storage.Used + "/" + storage.Room));
                if (storage.Bodies > 0) camp.Add(Body(Loc.T("camp.bodies") + " " + storage.Bodies));
                if (storage.Cells > 0)
                {
                    camp.Add(Button(Loc.T("camp.cell") + " " + storage.Cells, () =>
                    {
                        var pack = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
                        var record = ItemCatalog.Find("cell");
                        if (pack == null || record == null || ColonyStorage.Instance == null) return;
                        if (!pack.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight)) return;
                        ColonyStorage.Instance.TakeCell();
                    }));
                }
            }
            var roster = SurvivorRoster.Instance;
            if (roster != null)
            {
                camp.Add(Body(Loc.T("camp.morale") + " " + Mathf.RoundToInt(roster.AverageMorale())));
                if (!string.IsNullOrEmpty(roster.DayNotes)) camp.Add(Body(NoteSay.Read(roster.DayNotes, null)));
                if (roster.Memorials.Count > 0)
                {
                    camp.Add(Body(Loc.T("camp.memorial")));
                    for (int i = 0; i < roster.Memorials.Count; i++) camp.Add(Body(SuccessionLedger.Card(roster.Memorials[i])));
                }
                int mates = 0;
                foreach (var survivor in roster.Survivors) mates++;
                var mateIds = new string[mates];
                var mateActs = new string[mates];
                var mateHere = new bool[mates];
                int mate = 0;
                foreach (var survivor in roster.Survivors)
                {
                    mateIds[mate] = survivor.id;
                    mateHere[mate] = survivor.alive && !survivor.leader;
                    mateActs[mate] = CampRoutine.Choose(survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury, survivor.fatigue);
                    mate++;
                }
                foreach (var survivor in roster.Survivors)
                {
                    string flag = survivor.leader ? "*" : survivor.alive ? "" : "x";
                    string mood = ColonyDay.Mood(survivor.morale, survivor.trait, survivor.aside, survivor.mark);
                    string doing = CampRoutine.Choose(survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury, survivor.fatigue);
                    string host = YardVisit.Host(survivor.id, doing, survivor.kin, survivor.fatigue, mateIds, mateActs, mateHere);
                    if (host.Length > 0) doing = "Visit";
                    string post = doing == survivor.task ? Loc.Task(survivor.task) : Loc.Task(survivor.task) + " → " + Loc.Task(doing);
                    if (survivor.ownCall) post += " (" + Loc.Task(TaskPick.Auto) + ")";
                    string skills = Practice.Line(survivor.combat, survivor.medicine, survivor.engineering, survivor.cooking, survivor.scavenge, null);
                    string leads = Heir.Line(survivor.leadership, null);
                    string wound = WoundCard.Line(survivor.injury, null);
                    camp.Add(Body(flag + " " + survivor.displayName + " (" + Marks(survivor) + ") " + post
                        + "  " + Loc.Mood(mood)
                        + (wound.Length > 0 ? "  " + wound : "")
                        + "  " + Loc.T("camp.food") + " " + Mathf.RoundToInt(survivor.hunger)
                        + " " + Loc.T("camp.water") + " " + Mathf.RoundToInt(survivor.thirst)
                        + (survivor.fatigue > 0f ? "  " + Loc.T("camp.wear") + " " + Mathf.RoundToInt(survivor.fatigue) + (NeedsPressure.Tired(survivor.fatigue) ? " " + Loc.T("camp.tired") : "") : "")
                        + "  " + survivor.bond
                        + "  " + Loc.T("camp.opinion") + " " + survivor.opinion
                        + Bonds(survivor.kin)
                        + (skills.Length > 0 ? "  " + skills : "")
                        + (leads.Length > 0 ? "  " + leads : "")
                        + (LifeLine.Line(survivor.age, survivor.past, null).Length > 0 ? "  " + LifeLine.Line(survivor.age, survivor.past, null) : "")
                        + "  \"" + Loc.Bark(doing, survivor.morale, survivor.fatigue) + "\""));
                    if (!survivor.alive) continue;
                    string id = survivor.id;
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.Add(Button(Loc.Task("Rest"), () => roster.Assign(id, "Rest")));
                    row.Add(Button(Loc.Task("Scavenge"), () => roster.Assign(id, "Scavenge")));
                    row.Add(Button(Loc.Task("Guard"), () => roster.Assign(id, "Guard")));
                    row.Add(Button(Loc.Task("Cook"), () => roster.Assign(id, "Cook")));
                    row.Add(Button(Loc.Task("Medic"), () => roster.Assign(id, "Medic")));
                    row.Add(Button(Loc.Task("Build"), () => roster.Assign(id, "Build")));
                    row.Add(Button(Loc.Task(CraftQueue.Task), () => roster.Assign(id, CraftQueue.Task)));
                    row.Add(Button(Loc.Task("Clear"), () => roster.Assign(id, "Clear")));
                    if (!survivor.leader) row.Add(Button(Loc.Task(TaskPick.Auto), () => roster.Assign(id, TaskPick.Auto)));
                    if (survivor.injury > 0) row.Add(Button(Loc.Task("Quarantine"), () => roster.Assign(id, "Quarantine")));
                    if (!survivor.leader) row.Add(Button(Loc.T("camp.gift"), () => roster.OfferMeal(id)));
                    camp.Add(Lit(row, TutorialMark.Task));
                }
            }
            if (FactionTrade.Instance != null)
            {
                var merchants = FactionTrade.Instance;
                string who = string.IsNullOrEmpty(merchants.ActiveId) ? Loc.T("camp.caravan") : merchants.ActiveId;
                string presence = string.IsNullOrEmpty(merchants.ActiveId) ? Loc.T("camp.away") : Loc.T("camp.at_gate");
                camp.Add(Body(CaravanBook.Display(who) + " — " + presence + "  " + merchants.StandingOf(who)));
            }
            camp.Add(Button(Loc.T("camp.caravan"), () => FactionTrade.Instance?.Toggle()));
            camp.Add(Button(Loc.T("camp.advance"), () =>
            {
                if (NightRaidController.Instance != null && NightRaidController.Instance.HoldWatch(6f)) return;
                WorldClock.Instance?.Advance(6f);
                SurvivorRoster.Instance?.TickTasks();
            }));
            int raidDay = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int raidSecurity = ColonyStorage.Instance != null ? ColonyStorage.Instance.Security : 0;
            int raidShots = ColonyStorage.Instance != null ? ColonyStorage.Instance.Shots : 0;
            bool endlessNights = WorldMapService.Instance != null && WorldMapService.Instance.Endless;
            bool generatorRunning = CampServices.Instance != null && CampServices.Instance.GeneratorOnline;
            int walls = GridBuilder.Instance != null ? GridBuilder.Instance.BarricadeCount() : 0;
            int difficulty = WorldMapService.Instance != null ? WorldMapService.Instance.Difficulty : 2;
            bool raidLikely = RaidCall.Likely(raidDay, raidSecurity, endlessNights, raidShots, generatorRunning, walls, difficulty);
            camp.Add(Body((raidLikely ? Loc.T("camp.raid_yes") : Loc.T("camp.raid_no")) + "  " + Loc.T("camp.shots") + " " + raidShots));
            camp.Add(Button(Loc.T("camp.endure"), () => NightRaidController.Instance?.Begin()));
            if (NightRaidController.Instance != null && NightRaidController.Instance.Warning)
                camp.Add(Body(Loc.T("camp.warn") + " " + Mathf.CeilToInt(NightRaidController.Instance.WarningLeft)));
            camp.Add(Body(Loc.T("camp.build") + " " + (GridBuilder.Instance != null ? GridBuilder.Instance.Selected + "  " + GridBuilder.Instance.Facing : "")));
            if (TutorialDirector.Instance != null && TutorialDirector.Instance.CampMark == TutorialMark.Barricade) buildTab = BuildMenu.Tab.Defence;
            var tabs = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            for (int t = 0; t < BuildMenu.TabCount; t++)
            {
                var tab = (BuildMenu.Tab)t;
                var tabButton = Button(Loc.T(BuildMenu.TabKey(tab)), () => buildTab = tab);
                if (tab == buildTab) tabButton.style.backgroundColor = new Color(0.32f, 0.3f, 0.22f);
                tabs.Add(tabButton);
            }
            camp.Add(tabs);
            var build = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            foreach (var kind in BuildMenu.Kinds(buildTab))
            {
                var pick = kind;
                var bill = GridBuilder.Bill(kind);
                var moduleButton = Button(bill.Button(Loc.T(BuildMenu.LabelKey(kind))), () => GridBuilder.Instance?.Select(pick));
                if (!BuildMenu.Affordable(bill, ColonyStorage.Instance)) moduleButton.style.color = new Color(0.55f, 0.5f, 0.48f);
                if (GridBuilder.Instance != null && GridBuilder.Instance.Selected == kind) moduleButton.style.backgroundColor = new Color(0.25f, 0.32f, 0.22f);
                build.Add(kind == ModuleKind.Barricade ? Lit(moduleButton, TutorialMark.Barricade) : moduleButton);
            }
            if (GridBuilder.Instance != null)
            {
                int sprout = -1;
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    if (module.kind != "Farm" || module.site != 0 || module.integrity <= 0 || module.age >= CampYield.FarmWait) continue;
                    if (sprout < 0 || module.age < sprout) sprout = module.age;
                }
                if (sprout >= 0) camp.Add(Body(Loc.T("camp.sprout") + " " + sprout + "/" + CampYield.FarmWait));
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    if (module.site == 0 || module.integrity <= 0) continue;
                    camp.Add(Body(Loc.T("camp.raising") + " " + module.kind + " " + module.hours + "/" + BuildSite.Need(module.kind)));
                }
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    if (!MendBoard.Needs(module.site, module.integrity)) continue;
                    camp.Add(Body(Loc.T("camp.mend_module") + " " + module.kind + " " + module.integrity));
                }
            }
            camp.Add(build);
            if (GridBuilder.Instance != null && GridBuilder.Instance.CountKind("Turret") > 0 && GridBuilder.Instance.BenchTier() < 2)
                camp.Add(Body(Loc.T("camp.turret_tier")));
            camp.Add(Body(Loc.T("camp.craft")));
            bool bench = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Workbench");
            int benchTier = GridBuilder.Instance != null ? GridBuilder.Instance.BenchTier() : 1;
            string prints = storage != null ? storage.Prints : "";
            if (prints.Length > 0)
            {
                var plans = Loc.T("camp.plans");
                var known = CraftGate.Ids(prints);
                for (int i = 0; i < known.Length; i++) plans += "  " + Loc.T("print." + known[i]);
                camp.Add(Body(plans));
            }
            var leaderPack = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            int carriedTier = leaderPack != null ? leaderPack.PackTier : 1;
            if (carriedTier >= 2) camp.Add(Body(Loc.T("camp.pack_t2")));
            else if (bench && benchTier >= 2)
            {
                camp.Add(Button(Loc.T("camp.pack_raise") + "  " + PackOps.RaiseScrap + "  " + Loc.T("camp.cloth") + " " + PackOps.RaiseCloth + "  " + Loc.T("camp.tape") + " " + PackOps.RaiseTape, () =>
                {
                    int tierNow = GridBuilder.Instance != null ? GridBuilder.Instance.BenchTier() : 1;
                    PlayerRegistry.Current?.GetComponent<PlayerInventory>()?.TryRaisePack(tierNow, ColonyStorage.Instance);
                }));
            }
            if (bench && benchTier >= 2) camp.Add(Body(Loc.T("camp.bench_t2")));
            var heldGun = PlayerRegistry.Current != null ? PlayerRegistry.Current.ActiveWeapon as FirearmWeapon : null;
            string heldId = heldGun != null ? heldGun.CardId : "";
            int stripScrap = StripYield.Scrap(heldId);
            int stripCount = PlayerRegistry.Current != null ? PlayerRegistry.Current.WeaponCount : 0;
            bool stripMelee = PlayerRegistry.Current != null && PlayerRegistry.Current.ActiveWeapon != null && PlayerRegistry.Current.ActiveWeapon.Type == WeaponType.Melee;
            if (StripYield.Can(stripCount, stripMelee, bench) && stripScrap > 0)
            {
                camp.Add(Button(Loc.T("camp.strip") + "  " + stripScrap, () =>
                {
                    var who = PlayerRegistry.Current;
                    var gun = who != null ? who.ActiveWeapon as FirearmWeapon : null;
                    string card = gun != null ? gun.CardId : "";
                    int due = StripYield.Scrap(card);
                    int chem = StripYield.Chemicals(card);
                    var bin = ColonyStorage.Instance;
                    if (who == null || bin == null || due <= 0 || !StripYield.RoomFor(bin.Used, bin.Room, due, chem))
                    {
                        GameplayFeedback.Toast(Loc.T(due > 0 ? "camp.strip_full" : "camp.strip_none"));
                        return;
                    }
                    if (!who.TryStrip())
                    {
                        GameplayFeedback.Toast(Loc.T("camp.strip_none"));
                        return;
                    }
                    bin.AddScrap(due);
                    if (chem > 0) bin.AddChemicals(chem);
                    GameplayFeedback.Toast(Loc.T("camp.strip_ok"));
                }));
            }
            else if (bench && GridBuilder.Instance.BenchOrdered()) camp.Add(Body(Loc.T("camp.bench_raise") + " " + GridBuilder.Instance.BenchWork() + "/" + CraftGate.Hours));
            else if (bench) camp.Add(Button(Loc.T("camp.bench_raise") + "  " + CraftGate.UpgradeScrap, () => GridBuilder.Instance.OrderBench()));
            var benchOrders = CraftingBench.Instance != null ? CraftingBench.Instance.Orders : null;
            if (benchOrders != null && benchOrders.Count > 0)
                camp.Add(Body(Loc.T("craft.orders") + " " + CraftQueue.Line(benchOrders, order => Loc.Recipe(order, order))));
            foreach (var recipe in CraftingBench.Recipes)
            {
                string id = recipe.Id;
                if (!CraftGate.Open(id, benchTier, prints)) continue;
                if (!CraftBill.TryOf(id, out var bill)) continue;
                int due = CraftingBench.Priced(bill.Scrap, bench, benchTier);
                string line = CraftSay.Line(Loc.Recipe(id, recipe.Label), due, bill.Cloth, bill.Chemicals, bill.Tape, null);
                if (bill.Raw > 0) line += "   " + Loc.T("camp.raw") + " " + bill.Raw;
                var craft = Button(line, () => CraftingBench.Instance?.Craft(id));
                var recipeRow = new VisualElement();
                recipeRow.style.flexDirection = FlexDirection.Row;
                recipeRow.Add(id == TutorialMark.Bandage ? Lit(craft, TutorialMark.Bandage) : craft);
                if (bench && CraftQueue.Orderable(id)) recipeRow.Add(Button(Loc.T("craft.queue"), () => CraftingBench.Instance?.Order(id)));
                camp.Add(recipeRow);
            }
            if (bench && leaderPack != null)
            {
                foreach (var carried in leaderPack.Items)
                {
                    if (carried == null || carried.Quantity <= 0) continue;
                    if (!CraftBill.Dismantle(carried.ItemId, out int bits, out int rags, out int chems, out int tapes)) continue;
                    string itemId = carried.ItemId;
                    string yield = CraftSay.Line(Loc.T("camp.dismantle") + " " + Loc.Recipe(itemId, carried.ItemName) + " x" + carried.Quantity, bits, rags, chems, tapes, null);
                    camp.Add(Button(yield, () => CraftingBench.Instance?.Dismantle(itemId)));
                }
            }
            var map = WorldMapService.Instance;
            if (map != null)
            {
                camp.Add(Body(Loc.T("camp.radio") + " " + CampaignBoard.PartCount(map.Parts) + "/3  " + Loc.Difficulty(map.Difficulty) + "  " + Loc.T("camp.seed") + " " + map.WorldSeed));
                camp.Add(Button(Loc.T("camp.reroll"), () => map.RerollSeed()));
                if (map.Endless) camp.Add(Body(Loc.T("camp.broadcast_holds")));
                else if (map.CampaignWon) camp.Add(Body(Loc.T("camp.tower_air")));
                else if (map.ReadyToBroadcast) camp.Add(Button(Loc.T("camp.broadcast"), () => NightRaidController.Instance?.BeginBroadcast()));
                else camp.Add(Body(Loc.T("camp.tower_needs")));
                camp.Add(Body(Loc.T("camp.district")));
                string[] charted = ClearedDistricts(map);
                int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
                int hidden = MapVeil.Hidden(charted);
                foreach (var district in map.Districts)
                {
                    if (!MapVeil.Seen(district.id, charted)) continue;
                    string cast = SkyCast(district.id, charted, day) + SiteCast(district.id, charted) + SearchedCast(map, district.id);
                    if (district.cleared && !map.Endless)
                    {
                        string part = CampaignBoard.PartFor(district.id);
                        camp.Add(Body(Loc.District(district.id) + " — " + Loc.T("camp.clear") + (string.IsNullOrEmpty(part) ? "" : "  " + Loc.T("camp.part")) + cast));
                        continue;
                    }
                    string id = district.id;
                    if (!CampaignBoard.Reachable(id, charted))
                    {
                        camp.Add(Body(Loc.District(id) + " — " + Loc.T("camp.closed") + cast));
                        continue;
                    }
                    string mark = map.Current != null && map.Current.id == id ? "> " : "";
                    string hours = CampaignBoard.TravelHours(id).ToString("0");
                    string burn = FuelTank.Label(FuelTank.TripCost(CampaignBoard.TravelHours(id)));
                    camp.Add(Button(mark + Loc.District(id) + "  " + hours + "h  " + Loc.T("camp.fuel") + " " + burn + cast, () => map.Select(id)));
                }
                if (hidden > 0) camp.Add(Body(Loc.T("camp.fog") + "  " + hidden));
            }
            camp.Add(Lit(Button(Loc.T("camp.leave"), () => Go(FlowStep.Expedition)), TutorialMark.Leave));
            ShadeCamp();
        }

        private T Lit<T>(T element, string tag) where T : VisualElement
        {
            if (!TutorialMark.Lit(campMark, tag)) return element;
            var ring = new Color(0.95f, 0.72f, 0.3f);
            element.style.borderTopWidth = TutorialMark.Ring;
            element.style.borderBottomWidth = TutorialMark.Ring;
            element.style.borderLeftWidth = TutorialMark.Ring;
            element.style.borderRightWidth = TutorialMark.Ring;
            element.style.borderTopColor = ring;
            element.style.borderBottomColor = ring;
            element.style.borderLeftColor = ring;
            element.style.borderRightColor = ring;
            campLit.Add(element);
            return element;
        }

        private void ShadeCamp()
        {
            var holders = new HashSet<VisualElement>();
            foreach (var lit in campLit)
            {
                var node = lit;
                while (node != null && node.parent != camp) node = node.parent;
                if (node != null) holders.Add(node);
            }
            foreach (var child in camp.Children())
                child.style.opacity = TutorialMark.Opacity(campLit.Count > 0, holders.Contains(child), child == guideBox);
            if (campLit.Count == 0 || !InputGlyphs.UsingPad) return;
            var first = campLit[0] as Button ?? campLit[0].Q<Button>();
            first?.Focus();
        }

        private void RebuildPack(bool open)
        {
            pack.Clear();
            pack.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (!open) return;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            pack.Add(Title(Loc.T("camp.pack")));
            if (inventory == null) return;
            pack.Add(Body(Loc.T("camp.weight") + " " + inventory.CurrentWeight.ToString("0.0") + " / " + inventory.MaxWeightCapacity.ToString("0.0")));
            var track = new VisualElement();
            track.style.height = 8;
            track.style.marginBottom = 8;
            track.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            var fill = new VisualElement();
            float ratio = Mathf.Clamp01(inventory.WeightRatio);
            fill.style.width = Length.Percent(ratio * 100f);
            fill.style.height = 8;
            bool heavy = PackOps.Heavy(inventory.CurrentWeight, inventory.MaxWeightCapacity);
            int vision = SettingsService.Instance != null ? SettingsService.Instance.ColorblindMode : 0;
            fill.style.backgroundColor = heavy ? HudPalette.Health(vision) : HudPalette.Safe(vision);
            track.Add(fill);
            pack.Add(track);
            var gear = inventory.GetComponent<PlayerController>();
            if (gear != null)
            {
                var slots = Body();
                slots.style.whiteSpace = WhiteSpace.PreWrap;
                slots.text = Loc.T("pack.gear") + "\n" + gear.GearLine() + "\n" + Loc.T("camp.belt") + " " + inventory.BeltLine + "\n" + Loc.T("pack.tier") + " " + inventory.PackTier;
                pack.Add(slots);
            }
            pack.Add(Button(Loc.T("pack.filter") + ": " + Loc.T(PackFilter.Key(packFilter)), () => packFilter = PackFilter.Next(packFilter)));
            if (inventory.MedicalKits > 0)
            {
                var medRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var medLabel = Body();
                medLabel.text = Loc.Item("medkit") + " x" + inventory.MedicalKits;
                medLabel.style.flexGrow = 1;
                medRow.Add(medLabel);
                medRow.Add(Button(Loc.T("camp.use"), () =>
                {
                    if (inventory.UseMedkit())
                        GameplayFeedback.Toast(WoundEase.Note(FieldHand.Dose(inventory.LastDoseSkill, null), inventory.LastEase, null));
                }));
                medRow.Add(Button(Loc.T("camp.info"), () => Inspect("medkit")));
                string medMark = inventory.BeltMark("medkit");
                medRow.Add(Button(string.IsNullOrEmpty(medMark) ? Loc.T("camp.belt") : Loc.T("camp.belt") + " " + medMark, () => inventory.ToggleBelt("medkit")));
                pack.Add(medRow);
            }
            if (inventory.ScrapCount > 0)
            {
                var scrapRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var scrapLabel = Body();
                scrapLabel.text = Loc.Item("scrap") + " x" + inventory.ScrapCount;
                scrapLabel.style.flexGrow = 1;
                scrapRow.Add(scrapLabel);
                scrapRow.Add(Button(Loc.T("camp.info"), () => Inspect("scrap")));
                pack.Add(scrapRow);
            }
            var crate = LootContainer.Open;
            var scroll = new ScrollView();
            scroll.style.height = 220;
            foreach (var item in inventory.Items)
            {
                if (!PackFilter.Shows(packFilter, item.Category)) continue;
                string id = item.ItemId;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var icon = ItemDatabase.Icon(id);
                if (icon != null)
                {
                    row.Add(Picture(icon, 32));
                }
                var label = Body();
                label.text = Loc.Item(id) + " x" + item.Quantity;
                label.style.flexGrow = 1;
                row.Add(label);
                row.Add(Button(Loc.T("camp.use"), () => inventory.TryUse(id)));
                row.Add(Button(Loc.T("camp.info"), () => Inspect(id)));
                row.Add(Button(Loc.T("camp.drop"), () => inventory.Drop(id, 1)));
                row.Add(Button(Loc.T("camp.split"), () => inventory.DropHalf(id)));
                if (ItemBelt.Fits(id))
                {
                    string mark = inventory.BeltMark(id);
                    row.Add(Button(string.IsNullOrEmpty(mark) ? Loc.T("camp.belt") : Loc.T("camp.belt") + " " + mark, () => inventory.ToggleBelt(id)));
                }
                if (crate != null && crate.HeldCount > 0) row.Add(Button(Loc.T("pack.stow"), () => crate.Stow(id, inventory)));
                scroll.Add(row);
            }
            pack.Add(scroll);
            if (crate != null && crate.HeldCount > 0)
            {
                pack.Add(Body(Loc.T("camp.container")));
                for (int i = 0; i < crate.HeldCount; i++)
                {
                    string id = crate.HeldId(i);
                    pack.Add(Button(Loc.T("camp.take") + " " + crate.HeldOffer(i), () => crate.Take(id, inventory)));
                }
                pack.Add(Button(Loc.T("camp.take_all"), () => crate.TakeAll(inventory)));
            }
            if (!string.IsNullOrEmpty(inspected))
            {
                var detail = Body();
                detail.style.whiteSpace = WhiteSpace.PreWrap;
                detail.text = ItemBrief.Text(ItemCatalog.Find(inspected));
                pack.Add(detail);
            }
            pack.Add(Button(Loc.T("set.close"), () => FindFirstObjectByType<GameShellUI>()?.CloseInventory()));
        }

        private void Inspect(string id)
        {
            inspected = inspected == id ? "" : id ?? "";
        }

        private void DrawSlots(VisualElement menu)
        {
            menu.Add(Title(MenuLine.Title(null)));
            var cards = SaveSystem.Instance != null ? SaveSystem.Instance.Cards() : System.Array.Empty<SaveSlots.Card>();
            for (int i = 0; i < SaveSlots.ManualCount; i++)
            {
                SaveSlots.Card card = default;
                for (int c = 0; c < cards.Length; c++)
                {
                    if (cards[c].Slot == i) card = cards[c];
                }
                int index = i;
                string label = MenuLine.Slot(i + 1, card.Day, card.Leader, card.Occupied, null);
                menu.Add(Button(label, () =>
                {
                    screens.Clear();
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
                menu.Add(Button(MenuLine.Auto(auto.Day, auto.Leader, null), () =>
                {
                    screens.Clear();
                    Go(FlowStep.Sanctuary, () =>
                    {
                        if (SaveSystem.Instance == null || !SaveSystem.Instance.LoadSlot(SaveSlots.AutoSlot))
                            GameManager.Instance.SetState(GameState.MainMenu);
                    });
                }));
            }
            menu.Add(Button(Loc.T("menu.back"), Close));
        }

        private static string StormNote(CampServices services)
        {
            if (services == null || !services.GeneratorOnline) return "";
            var sky = WeatherController.Instance;
            if (sky == null || sky.Kind != WeatherKind.Storm) return "";
            return "  " + Loc.T("tank.storm");
        }

        private static string SkyCast(string id, string[] charted, int day)
        {
            string sky = MapVeil.Forecast(id, charted, day);
            if (string.IsNullOrEmpty(sky)) return "";
            return "  " + Loc.T("sky." + sky);
        }

        private static string SiteCast(string id, string[] charted)
        {
            string site = MapVeil.Site(id, charted);
            if (string.IsNullOrEmpty(site)) return "";
            return "  " + Loc.T("poi." + site);
        }

        private static string SearchedCast(WorldMapService map, string id)
        {
            int searched = StreetLedger.Count(map.Street, id);
            if (searched <= 0) return "";
            return "  " + searched + " " + Loc.T("camp.searched");
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

        private const int Closed = int.MinValue;

        private int CampKey()
        {
            var key = new UiKey();
            key.Add(SettingsService.Instance != null ? SettingsService.Instance.Language : "en");
            if (TutorialDirector.Instance != null)
            {
                key.Add(TutorialDirector.Instance.CampIndex);
                key.Add(InputGlyphs.UsingPad);
            }
            if (WorldClock.Instance != null) key.Add(WorldClock.Instance.Day);
            var storage = ColonyStorage.Instance;
            if (storage != null)
            {
                key.Add(storage.Scrap);
                key.Add((int)buildTab);
                if (GridBuilder.Instance != null) key.Add((int)GridBuilder.Instance.Selected);
                key.Add(storage.Food);
                key.Add(storage.Water);
                key.Add(storage.Security);
                key.Add(storage.Cloth);
                key.Add(storage.Chemicals);
                key.Add(storage.Tape);
                key.Add(storage.Raw);
            }
            var roster = SurvivorRoster.Instance;
            if (roster != null)
            {
                var survivors = roster.Survivors;
                for (int i = 0; i < survivors.Count; i++)
                {
                    var survivor = survivors[i];
                    key.Add(survivor.id);
                    key.Add(survivor.task);
                    key.Add(survivor.alive);
                    key.Add(survivor.leader);
                    key.Add(Mathf.RoundToInt(survivor.morale));
                    key.Add(Mathf.RoundToInt(survivor.hunger));
                    key.Add(survivor.opinion);
                    key.Add(survivor.injury);
                    key.Add(survivor.leadership);
                    key.Add(survivor.ownCall);
                    key.Add(survivor.combat);
                    key.Add(survivor.medicine);
                    key.Add(survivor.engineering);
                    key.Add(survivor.cooking);
                    key.Add(survivor.scavenge);
                }
                key.Add(roster.DayNotes);
                var memorials = roster.Memorials;
                key.Add(memorials.Count);
                for (int i = 0; i < memorials.Count; i++) key.Add(memorials[i] != null ? memorials[i].name : null);
            }
            var grid = GridBuilder.Instance;
            if (grid != null)
            {
                key.Add((int)grid.Selected);
                key.Add(grid.Facing);
                var placed = grid.Placed;
                for (int i = 0; i < placed.Count; i++)
                {
                    var module = placed[i];
                    key.Add(module.kind);
                    key.Add(module.age);
                    key.Add(module.integrity);
                    key.Add(module.site);
                    key.Add(module.hours);
                }
            }
            if (CampServices.Instance != null) key.Add(CampServices.Instance.GeneratorOnline);
            var map = WorldMapService.Instance;
            if (map != null && map.Current != null)
            {
                key.Add(map.Current.id);
                key.Add(map.Parts);
                key.Add(map.BroadcastWon);
                key.Add(map.ClearedCount);
                key.Add(map.WorldSeed);
                key.Add(map.Street);
            }
            if (FactionTrade.Instance != null) key.Add(FactionTrade.Instance.Key);
            if (CraftingBench.Instance != null) key.Add(CraftingBench.Instance.PackedOrders);
            return key.Value;
        }

        private int PackKey()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            var key = new UiKey();
            key.Add(inspected);
            key.Add(packFilter);
            if (inventory == null) return key.Value;
            key.Add(SettingsService.Instance != null ? SettingsService.Instance.Language : "en");
            var items = inventory.Items;
            for (int i = 0; i < items.Count; i++)
            {
                key.Add(items[i].ItemId);
                key.Add(items[i].Quantity);
            }
            key.Add(inventory.MedicalKits);
            key.Add(inventory.ScrapCount);
            key.Add(Mathf.RoundToInt(inventory.CurrentWeight * 10f));
            if (LootContainer.Open != null) key.Add(LootContainer.Open.Contents);
            key.Add(inventory.BeltLine);
            var body = inventory.GetComponent<PlayerController>();
            if (body != null) key.Add(body.GearLine());
            return key.Value;
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

        private static Label Title(string text)
        {
            var label = Body();
            label.text = text;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = 18;
            label.style.marginBottom = 8;
            return label;
        }

        private static VisualElement Picture(Texture2D icon, float side)
        {
            var picture = new VisualElement();
            picture.style.width = side;
            picture.style.height = side;
            picture.style.marginRight = 6;
            picture.style.flexShrink = 0;
            picture.style.backgroundImage = new StyleBackground(icon);
            return picture;
        }

        private static Label Body(string text)
        {
            var label = Body();
            label.text = text;
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

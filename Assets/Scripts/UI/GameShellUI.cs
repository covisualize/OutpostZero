using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    public static class ShellUi
    {
        public static bool OwnsMenus { get; set; }
    }

    public class GameShellUI : MonoBehaviour
    {
        private bool inventoryOpen;
        private Vector2 inventoryScroll;

        private void Awake()
        {
            ShellUi.OwnsMenus = true;
        }

        private void Update()
        {
            if (ExpeditionInput.InventoryPressed)
            {
                inventoryOpen = !inventoryOpen;
            }
        }

        private void OnGUI()
        {
            var scale = SettingsService.Instance != null ? SettingsService.Instance.TextScale : 1f;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawObjectives();
            DrawNeeds();
            if (inventoryOpen) DrawInventory();
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
            if (state == GameState.Paused) DrawPause();
            else if (state == GameState.CampManagement) DrawCamp();
            else if (state == GameState.SuccessionScreen) DrawSuccession();
            else if (state == GameState.ExpeditionResults) DrawResults();
            else if (state == GameState.GameOver) DrawGameOver();
            else if (state == GameState.MainMenu) DrawMainMenu();
            else DrawTutorial();
            if (SettingsService.Instance != null && SettingsService.Instance.ShowSettings) DrawSettings();
            if (FactionTrade.Instance != null && FactionTrade.Instance.Open) DrawTrade();
            GUI.matrix = previous;
        }

        private void DrawObjectives()
        {
            var tracker = ObjectiveTracker.Instance;
            if (tracker == null) return;
            GUILayout.BeginArea(new Rect(20, 140, 340, 90), GUI.skin.box);
            GUILayout.Label("Kills " + tracker.Kills + "/" + tracker.KillGoal + "   Scrap " + tracker.Scrap + "/" + tracker.ScrapGoal);
            var district = WorldMapService.Instance != null ? WorldMapService.Instance.Current : null;
            if (district != null) GUILayout.Label(district.displayName + " — " + district.encounter);
            var horde = HordeDirector.Instance;
            if (horde != null) GUILayout.Label("Tension " + Mathf.RoundToInt(horde.Tension) + "  " + horde.State);
            var clock = WorldClock.Instance;
            if (clock != null) GUILayout.Label(clock.Label);
            var interactor = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInteractor>() : null;
            if (interactor != null && !string.IsNullOrEmpty(interactor.Prompt)) GUILayout.Label("[E] " + interactor.Prompt);
            GUILayout.EndArea();
        }

        private void DrawNeeds()
        {
            var player = PlayerRegistry.Current;
            if (player == null) return;
            var needs = player.GetComponent<SurvivalNeeds>();
            var effects = player.GetComponent<StatusEffectController>();
            var visibility = player.GetComponent<PlayerVisibility>();
            GUILayout.BeginArea(new Rect(20, 240, 340, 78), GUI.skin.box);
            if (needs != null)
            {
                GUILayout.Label("Hunger " + Mathf.RoundToInt(needs.Hunger) + "  Thirst " + Mathf.RoundToInt(needs.Thirst) + "  Fatigue " + Mathf.RoundToInt(needs.Fatigue));
            }
            if (visibility != null) GUILayout.Label("Exposure " + Mathf.RoundToInt(visibility.Exposure * 100f) + "%");
            if (effects != null)
            {
                string tags = effects.IsBleeding ? "Bleeding " : "";
                tags += effects.IsPoisoned ? "Poison " : "";
                tags += effects.IsInfected ? "Infection " : "";
                if (!string.IsNullOrEmpty(tags)) GUILayout.Label(tags);
            }
            var raid = NightRaidController.Instance;
            if (raid != null && raid.Running) GUILayout.Label("Raid " + Mathf.CeilToInt(raid.Remaining) + "s");
            GUILayout.EndArea();
        }

        private void DrawInventory()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 180, 80, 360, 360), GUI.skin.box);
            GUILayout.Label("PACK  [Tab]");
            if (inventory == null)
            {
                GUILayout.EndArea();
                return;
            }
            GUILayout.Label("Weight " + inventory.CurrentWeight.ToString("0.0") + " / " + inventory.MaxWeightCapacity.ToString("0.0"));
            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll, GUILayout.Height(240));
            foreach (var item in inventory.Items)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(item.ItemName + " x" + item.Quantity);
                if (GUILayout.Button("Use", GUILayout.Width(60))) inventory.TryUse(item.ItemId);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close")) inventoryOpen = false;
            GUILayout.EndArea();
        }

        private void DrawPause()
        {
            Dim();
            GUILayout.BeginArea(Panel(340, 380), GUI.skin.box);
            GUILayout.Label(Loc.T("menu.pause"));
            if (GUILayout.Button(Loc.T("menu.resume"), GUILayout.Height(32))) GameManager.Instance.TogglePause();
            if (GUILayout.Button(Loc.T("menu.save"), GUILayout.Height(32))) SaveSystem.Instance?.Save();
            if (GUILayout.Button(Loc.T("menu.camp"), GUILayout.Height(32))) GameManager.Instance.EnterCamp();
            if (GUILayout.Button(Loc.T("menu.settings"), GUILayout.Height(32))) SettingsService.Instance?.TogglePanel();
            if (GUILayout.Button(Loc.T("menu.main"), GUILayout.Height(32))) GameManager.Instance.SetState(GameState.MainMenu);
            if (GUILayout.Button("Restart", GUILayout.Height(32))) GameManager.Instance.RestartCurrentScene();
            GUILayout.EndArea();
        }

        private void DrawCamp()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 380, 20, 360, 520), GUI.skin.box);
            GUILayout.Label(Loc.T("camp.title"));
            var storage = ColonyStorage.Instance;
            if (storage != null) GUILayout.Label("Scrap " + storage.Scrap + "  Food " + storage.Food + "  Water " + storage.Water);
            var roster = SurvivorRoster.Instance;
            if (roster != null)
            {
                GUILayout.Label("Morale " + Mathf.RoundToInt(roster.AverageMorale()));
                foreach (var survivor in roster.Survivors)
                {
                    string flag = survivor.leader ? "*" : survivor.alive ? "" : "x";
                    GUILayout.Label(flag + " " + survivor.displayName + " (" + survivor.trait + ") " + survivor.task);
                    if (!survivor.alive) continue;
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Rest")) roster.Assign(survivor.id, "Rest");
                    if (GUILayout.Button("Scavenge")) roster.Assign(survivor.id, "Scavenge");
                    if (GUILayout.Button("Guard")) roster.Assign(survivor.id, "Guard");
                    if (GUILayout.Button("Cook")) roster.Assign(survivor.id, "Cook");
                    GUILayout.EndHorizontal();
                }
            }
            if (GUILayout.Button("Advance watch")) 
            {
                WorldClock.Instance?.Advance(6f);
                roster?.TickTasks();
            }
            if (GUILayout.Button("Endure the night")) NightRaidController.Instance?.Begin();
            GUILayout.Label("Build [B] then click. Selected " + (GridBuilder.Instance != null ? GridBuilder.Instance.Selected.ToString() : ""));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Barricade")) GridBuilder.Instance?.Select(ModuleKind.Barricade);
            if (GUILayout.Button("Cot")) GridBuilder.Instance?.Select(ModuleKind.Cot);
            if (GUILayout.Button("Water")) GridBuilder.Instance?.Select(ModuleKind.Water);
            if (GUILayout.Button("Tower")) GridBuilder.Instance?.Select(ModuleKind.Watchtower);
            GUILayout.EndHorizontal();
            GUILayout.Label("Craft");
            if (CraftingBench.Recipes != null)
            {
                foreach (var recipe in CraftingBench.Recipes)
                {
                    if (GUILayout.Button(recipe.Label + " (" + recipe.ScrapCost + ")")) CraftingBench.Instance?.Craft(recipe.Id);
                }
            }
            if (GUILayout.Button("Return to expedition")) GameManager.Instance.BeginExpedition();
            GUILayout.EndArea();
        }

        private void DrawSuccession()
        {
            Dim();
            GUILayout.BeginArea(Panel(460, 240), GUI.skin.box);
            GUILayout.Label("LEADER KILLED");
            var next = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Leader : null;
            GUILayout.Label(next != null
                ? "The gate passes on. Choose who walks out next."
                : "No one is left to hold the gate.");
            if (SurvivorRoster.Instance != null)
            {
                foreach (var survivor in SurvivorRoster.Instance.Survivors)
                {
                    if (!survivor.alive || survivor.leader) continue;
                    if (GUILayout.Button("Take " + survivor.displayName + " — " + survivor.trait, GUILayout.Height(32)))
                    {
                        GameManager.Instance.AcceptSuccessor(survivor.id);
                    }
                }
            }
            if (GUILayout.Button("Game over")) GameManager.Instance.SetState(GameState.GameOver);
            GUILayout.EndArea();
        }

        private void DrawResults()
        {
            Dim();
            GUILayout.BeginArea(Panel(420, 220), GUI.skin.box);
            GUILayout.Label(Loc.T("result.title"));
            var map = WorldMapService.Instance;
            GUILayout.Label(map != null && map.CampaignWon ? "The district ring is clear." : "Supplies are back inside the gate.");
            if (map != null && map.Current != null) GUILayout.Label("Next: " + map.Current.displayName);
            if (GUILayout.Button("Enter sanctuary", GUILayout.Height(36))) GameManager.Instance.EnterCamp();
            GUILayout.EndArea();
        }

        private void DrawGameOver()
        {
            Dim();
            GUILayout.BeginArea(Panel(420, 180), GUI.skin.box);
            GUILayout.Label(Loc.T("gameover.title"));
            GUILayout.Label("Every name on the roster is gone.");
            if (GUILayout.Button("New outpost", GUILayout.Height(36))) GameManager.Instance.RestartCurrentScene();
            GUILayout.EndArea();
        }

        private void DrawMainMenu()
        {
            Dim();
            GUILayout.BeginArea(Panel(380, 280), GUI.skin.box);
            GUILayout.Label("OUTPOST ZERO");
            if (GUILayout.Button("Continue expedition", GUILayout.Height(34)))
            {
                if (SaveSystem.Instance != null && SaveSystem.Instance.Load()) return;
                GameManager.Instance.SetState(GameState.ExpeditionActive);
            }
            if (GUILayout.Button("New expedition", GUILayout.Height(34))) GameManager.Instance.RestartCurrentScene();
            if (GUILayout.Button(Loc.T("menu.settings"), GUILayout.Height(34))) SettingsService.Instance?.TogglePanel();
            if (GUILayout.Button("Back to the street", GUILayout.Height(34))) GameManager.Instance.SetState(GameState.ExpeditionActive);
            GUILayout.EndArea();
        }

        private void DrawTutorial()
        {
            var tutorial = TutorialDirector.Instance;
            if (tutorial == null || tutorial.Finished || string.IsNullOrEmpty(tutorial.Current)) return;
            if (SettingsService.Instance != null && !SettingsService.Instance.Subtitles) return;
            GUI.Box(new Rect(Screen.width * 0.5f - 260, Screen.height - 120, 520, 48), tutorial.Current);
        }

        private void DrawSettings()
        {
            var settings = SettingsService.Instance;
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 160, 40, 320, 220), GUI.skin.box);
            GUILayout.Label("SETTINGS");
            GUILayout.Label("Shake " + settings.ScreenShake.ToString("0.00"));
            settings.SetShake(GUILayout.HorizontalSlider(settings.ScreenShake, 0f, 1f));
            GUILayout.Label("Volume " + settings.MasterVolume.ToString("0.00"));
            settings.SetVolume(GUILayout.HorizontalSlider(settings.MasterVolume, 0f, 1f));
            GUILayout.Label("Text " + settings.TextScale.ToString("0.00"));
            settings.SetTextScale(GUILayout.HorizontalSlider(settings.TextScale, 0.8f, 1.6f));
            if (GUILayout.Button(settings.Subtitles ? "Subtitles on" : "Subtitles off")) settings.SetSubtitles(!settings.Subtitles);
            if (GUILayout.Button("Colorblind mode " + settings.ColorblindMode)) settings.CycleColorblind();
            if (GUILayout.Button(settings.Language == "es" ? "Idioma: ES" : "Language: EN")) settings.SetLanguage(settings.Language == "es" ? "en" : "es");
            if (GUILayout.Button("Close")) settings.TogglePanel();
            GUILayout.EndArea();
        }

        private void DrawTrade()
        {
            var trade = FactionTrade.Instance;
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 80, 300, 180), GUI.skin.box);
            GUILayout.Label(trade.Faction + "  standing " + trade.Standing);
            if (GUILayout.Button("Buy medkit (" + trade.Price("medkit") + ")")) trade.Buy("medkit");
            if (GUILayout.Button("Buy rifle ammo (" + trade.Price("ammo_rifle") + ")")) trade.Buy("ammo_rifle");
            if (GUILayout.Button("Buy water (" + trade.Price("water") + ")")) trade.Buy("water");
            if (GUILayout.Button("Leave")) trade.Toggle();
            GUILayout.EndArea();
        }

        private static void Dim()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Rect Panel(float width, float height)
        {
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }
    }
}

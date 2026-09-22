using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private int slot;

        public int Slot => slot;
        public string PathToSave => PathFor(slot);

        public void UseSlot(int index)
        {
            slot = SaveSlots.Manual(index);
        }

        public string PathFor(int index)
        {
            return Path.Combine(Application.persistentDataPath, SaveSlots.FileName(index));
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public bool Save() => Save(true);

        public bool Save(bool announce)
        {
            var data = Capture();
            int target = announce ? slot : SaveSlots.AutoSlot;
            if (!Write(PathFor(target), data))
            {
                GameplayFeedback.Toast("Save failed");
                return false;
            }
            if (announce) GameplayFeedback.Toast("Game saved");
            return true;
        }

        public bool Load()
        {
            var cards = Cards();
            int best = SaveSlots.Newest(cards);
            if (best < 0)
            {
                GameplayFeedback.Toast("No save file");
                return false;
            }
            return LoadCard(cards[best]);
        }

        public bool LoadSlot(int index)
        {
            return LoadCard(new SaveSlots.Card { Slot = index, Occupied = true, Auto = index == SaveSlots.AutoSlot });
        }

        public bool HasSave()
        {
            return SaveSlots.Newest(Cards()) >= 0;
        }

        public SaveSlots.Card[] Cards()
        {
            var cards = new SaveSlots.Card[SaveSlots.ManualCount + 2];
            for (int i = 0; i < SaveSlots.ManualCount; i++) cards[i] = Peek(i);
            cards[SaveSlots.AutoSlot] = Peek(SaveSlots.AutoSlot);
            cards[SaveSlots.ManualCount + 1] = Peek(SaveSlots.LegacySlot);
            return cards;
        }

        private bool LoadCard(SaveSlots.Card card)
        {
            if (!TryRead(PathFor(card.Slot), out var data))
            {
                GameplayFeedback.Toast(card.Occupied ? "Save could not be read" : "No save file");
                return false;
            }
            if (SaveSlots.Manual(data.slot) == data.slot) UseSlot(data.slot);
            else if (card.Slot >= 0 && card.Slot < SaveSlots.ManualCount) UseSlot(card.Slot);
            Apply(data);
            GameplayFeedback.Toast(card.Auto ? "Autosave loaded" : "Save loaded");
            return true;
        }

        private SaveSlots.Card Peek(int index)
        {
            var card = new SaveSlots.Card { Slot = index, Auto = index == SaveSlots.AutoSlot };
            if (!TryRead(PathFor(index), out var data)) return card;
            card.Occupied = true;
            card.Day = data.day;
            card.Hour = data.hour;
            card.Leader = LeaderName(data);
            return card;
        }

        private static string LeaderName(SaveGameData data)
        {
            if (data == null || data.survivors == null) return "";
            for (int i = 0; i < data.survivors.Length; i++)
            {
                if (data.survivors[i] != null && data.survivors[i].leader && !string.IsNullOrEmpty(data.survivors[i].displayName))
                    return data.survivors[i].displayName;
            }
            for (int i = 0; i < data.survivors.Length; i++)
            {
                if (data.survivors[i] != null && !string.IsNullOrEmpty(data.survivors[i].displayName))
                    return data.survivors[i].displayName;
            }
            return "";
        }

        private bool Write(string path, SaveGameData data)
        {
            try
            {
                string json = SaveCodec.Serialize(data);
                string tmp = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SaveSystem] " + ex.Message);
                return false;
            }
        }

        private static bool TryRead(string path, out SaveGameData data)
        {
            data = null;
            if (ReadFile(path, out data)) return true;
            return ReadFile(path + ".bak", out data);
        }

        private static bool ReadFile(string path, out SaveGameData data)
        {
            data = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            return SaveCodec.TryDeserialize(File.ReadAllText(path), out data, out _);
        }

        public SaveGameData Capture()
        {
            var data = new SaveGameData();
            data.slot = slot;
            if (WorldClock.Instance != null)
            {
                data.day = WorldClock.Instance.Day;
                data.hour = WorldClock.Instance.Hour;
            }
            if (ColonyStorage.Instance != null)
            {
                data.colonyScrap = ColonyStorage.Instance.Scrap;
                data.food = ColonyStorage.Instance.Food;
                data.water = ColonyStorage.Instance.Water;
                data.cloth = ColonyStorage.Instance.Cloth;
                data.chemicals = ColonyStorage.Instance.Chemicals;
                data.tape = ColonyStorage.Instance.Tape;
                data.raw = ColonyStorage.Instance.Raw;
                data.bodies = ColonyStorage.Instance.Bodies;
                data.rounds = ColonyStorage.Instance.Rounds;
                data.shots = ColonyStorage.Instance.Shots;
                data.prints = ColonyStorage.Instance.Prints;
            }
            if (CampServices.Instance != null)
            {
                data.fuel = FuelTank.Pack(CampServices.Instance.FuelHours);
                data.fuelSet = 1;
            }
            if (GameManager.Instance != null)
            {
                data.kills = GameManager.Instance.ZombiesKilled;
                data.lifetimeKills = GameManager.Instance.LifetimeKills;
            }
            if (WorldMapService.Instance != null)
            {
                data.districtsCleared = WorldMapService.Instance.ClearedCount;
                data.districtIndex = WorldMapService.Instance.CurrentIndex;
                data.radio = WorldMapService.Instance.Parts;
                data.difficulty = WorldMapService.Instance.Difficulty;
                data.broadcast = WorldMapService.Instance.BroadcastWon ? 1 : 0;
                data.worldSeed = WorldMapService.Instance.WorldSeed;
                data.endless = WorldMapService.Instance.Endless ? 1 : 0;
            }
            if (FactionTrade.Instance != null)
            {
                data.factionStanding = FactionTrade.Instance.Standing;
                data.factions = FactionTrade.Instance.Pack();
                data.quests = FactionTrade.Instance.Quests;
            }
            if (TutorialDirector.Instance != null) data.tutorialDone = TutorialDirector.Instance.Finished;
            if (CodexDirector.Instance != null) data.codex = CodexDirector.Instance.Packed;
            if (OutpostZero.Player.PlayerRegistry.Current != null) data.weaponMods = OutpostZero.Player.PlayerRegistry.Current.PackMods();
            if (SurvivorRoster.Instance != null)
            {
                data.memorial = SurvivorRoster.Instance.PackMemorials();
                data.corpses = SurvivorRoster.Instance.PackCorpses();
            }
            if (SettingsService.Instance != null)
            {
                data.language = SettingsService.Instance.Language;
                data.sfxVolume = SettingsService.Instance.SfxVolume;
                data.musicVolume = SettingsService.Instance.MusicVolume;
                data.ambienceVolume = SettingsService.Instance.AmbienceVolume;
                data.uiVolume = SettingsService.Instance.UiVolume;
                data.quality = SettingsService.Instance.Quality;
                data.vsync = SettingsService.Instance.VSync ? 1 : 0;
                data.fieldOfView = SettingsService.Instance.FieldOfView;
                data.bindings = OutpostZero.Player.ControlBindings.Pack();
                data.shake = SettingsService.Instance.ScreenShake;
                data.volume = SettingsService.Instance.MasterVolume;
                data.textScale = SettingsService.Instance.TextScale;
                data.subtitles = SettingsService.Instance.Subtitles;
                data.mercy = SettingsService.Instance.Merciful ? 1 : 0;
                data.nextDifficulty = SettingsService.Instance.NextDifficulty;
                data.goreLevel = SettingsService.Instance.Gore == 0 ? 3 : SettingsService.Instance.Gore;
                data.hitStop = SettingsService.Instance.HitStop ? 1 : 2;
                data.damageNumbers = SettingsService.Instance.DamageNumbers ? 1 : 2;
                data.hudOpacity = SettingsService.Instance.HudOpacity;
                data.brightness = SettingsService.Instance.Brightness;
                data.motionBlur = SettingsService.Instance.MotionBlur ? 1 : 0;
                data.windowMode = SettingsService.Instance.WindowMode;
                data.aimAssist = SettingsService.Instance.AimAssist;
                data.invertLook = SettingsService.Instance.InvertLook ? 1 : 0;
                data.crouchMode = SettingsService.Instance.CrouchMode;
                data.sprintMode = SettingsService.Instance.SprintMode;
                data.frameCap = SettingsService.Instance.FrameCap;
                data.resolution = SettingsService.Instance.Resolution;
            }
            if (SurvivorRoster.Instance != null)
            {
                var list = new List<SurvivorSave>();
                foreach (var survivor in SurvivorRoster.Instance.Survivors)
                {
                    list.Add(new SurvivorSave
                    {
                        id = survivor.id,
                        displayName = survivor.displayName,
                        trait = survivor.trait,
                        alive = survivor.alive,
                        leader = survivor.leader,
                        morale = survivor.morale,
                        hunger = survivor.hunger,
                        thirst = survivor.thirst,
                        opinion = survivor.opinion,
                        injury = survivor.injury,
                        needsTracked = true,
                        task = survivor.task,
                        bond = survivor.bond
                    });
                }
                data.survivors = list.ToArray();
            }
            if (GridBuilder.Instance != null)
            {
                var modules = new List<ModuleSave>();
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    modules.Add(new ModuleSave { kind = module.kind, x = module.x, z = module.z, rotation = module.rotation, integrity = module.integrity <= 0 ? 100 : module.integrity, age = module.age, site = module.site, hours = module.hours, tier = module.tier, job = module.job });
                }
                data.modules = modules.ToArray();
            }
            return data;
        }

        public void Apply(SaveGameData data)
        {
            if (data == null) return;
            WorldClock.Instance?.Set(data.day, data.hour);
            GameManager.Instance?.SetLifetimeKills(data.lifetimeKills);
            ColonyStorage.Instance?.Set(data.colonyScrap, data.food, data.water);
            ColonyStorage.Instance?.SetSupplies(data.cloth, data.chemicals, data.tape);
            ColonyStorage.Instance?.SetRaw(data.raw);
            ColonyStorage.Instance?.SetBodies(data.bodies);
            ColonyStorage.Instance?.SetRounds(data.rounds);
            ColonyStorage.Instance?.SetShots(data.shots);
            CampServices.Instance?.SetFuel(FuelTank.Unpack(data.fuel, data.fuelSet));
            ColonyStorage.Instance?.SetPrints(data.prints);
            FactionTrade.Instance?.Restore(data.factionStanding, data.factions, data.quests);
            SettingsService.Instance?.ApplySnapshot(data.shake, data.volume, data.textScale, data.subtitles, data.language);
            SettingsService.Instance?.ApplyPresentation(data.sfxVolume, data.musicVolume, data.quality, data.vsync, data.fieldOfView, data.bindings, data.ambienceVolume, data.uiVolume);
            SettingsService.Instance?.SetMerciful(data.mercy != 0);
            SurvivorRoster.Instance?.RestoreStory(data.memorial, data.corpses);
            TutorialDirector.Instance?.SetFinished(data.tutorialDone);
            CodexDirector.Instance?.Restore(data.codex);
            OutpostZero.Player.PlayerRegistry.Current?.RestoreMods(data.weaponMods);
            WorldMapService.Instance?.RestoreCleared(data.districtsCleared);
            WorldMapService.Instance?.RestoreCampaign(data.radio, data.difficulty, data.broadcast, data.worldSeed, data.endless);
            if (data.districtIndex > data.districtsCleared) WorldMapService.Instance?.SelectIndex(data.districtIndex);
            if (data.nextDifficulty > 0) SettingsService.Instance?.SetNextDifficulty(data.nextDifficulty);
            SettingsService.Instance?.ApplyComfort(data.goreLevel, data.hitStop, data.damageNumbers, data.hudOpacity, data.brightness, data.motionBlur, data.windowMode);
            SettingsService.Instance?.ApplyPlay(data.aimAssist, data.invertLook, data.crouchMode, data.sprintMode, data.frameCap, data.resolution);
            if (data.survivors != null && data.survivors.Length > 0 && SurvivorRoster.Instance != null)
            {
                var list = new List<Survivor>();
                foreach (var saved in data.survivors)
                {
                    list.Add(new Survivor
                    {
                        id = saved.id,
                        displayName = saved.displayName,
                        trait = saved.trait,
                        alive = saved.alive,
                        leader = saved.leader,
                        morale = saved.morale,
                        hunger = saved.needsTracked ? saved.hunger : 78f,
                        thirst = saved.needsTracked ? saved.thirst : 78f,
                        opinion = saved.needsTracked ? saved.opinion : (string.IsNullOrEmpty(saved.bond) ? 0 : 18),
                        injury = saved.needsTracked ? saved.injury : 0,
                        task = string.IsNullOrEmpty(saved.task) ? "Rest" : saved.task,
                        bond = saved.bond
                    });
                }
                SurvivorRoster.Instance.Replace(list);
            }
            if (GridBuilder.Instance != null)
            {
                var modules = new List<PlacedModule>();
                if (data.modules != null)
                {
                    foreach (var module in data.modules)
                    {
                        modules.Add(new PlacedModule { kind = module.kind, x = module.x, z = module.z, rotation = module.rotation, integrity = module.integrity <= 0 ? 100 : module.integrity, age = module.age, site = module.site, hours = module.hours, tier = module.tier, job = module.job });
                    }
                }
                GridBuilder.Instance.Restore(modules.ToArray());
            }
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.CampManagement);
        }
    }
}

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

        public string PathToSave => Path.Combine(Application.persistentDataPath, "outpost-zero-save.json");

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
            string json = SaveCodec.Serialize(data);
            try
            {
                string path = PathToSave;
                string tmp = path + ".tmp";
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Copy(tmp, path, true);
                File.Delete(tmp);
                if (announce) GameplayFeedback.Toast("Game saved");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SaveSystem] " + ex.Message);
                GameplayFeedback.Toast("Save failed");
                return false;
            }
        }

        public bool Load()
        {
            string path = PathToSave;
            if (!File.Exists(path))
            {
                GameplayFeedback.Toast("No save file");
                return false;
            }
            if (!SaveCodec.TryDeserialize(File.ReadAllText(path), out var data, out var error))
            {
                GameplayFeedback.Toast(error == "schema" ? "Save is from another version" : "Save could not be read");
                return false;
            }
            Apply(data);
            GameplayFeedback.Toast("Save loaded");
            return true;
        }

        public bool HasSave() => File.Exists(PathToSave);

        public SaveGameData Capture()
        {
            var data = new SaveGameData();
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
            }
            if (GameManager.Instance != null) data.kills = GameManager.Instance.ZombiesKilled;
            if (WorldMapService.Instance != null)
            {
                data.districtsCleared = WorldMapService.Instance.ClearedCount;
                data.districtIndex = WorldMapService.Instance.CurrentIndex;
            }
            if (FactionTrade.Instance != null) data.factionStanding = FactionTrade.Instance.Standing;
            if (TutorialDirector.Instance != null) data.tutorialDone = TutorialDirector.Instance.Finished;
            if (SettingsService.Instance != null)
            {
                data.language = SettingsService.Instance.Language;
                data.sfxVolume = SettingsService.Instance.SfxVolume;
                data.musicVolume = SettingsService.Instance.MusicVolume;
                data.quality = SettingsService.Instance.Quality;
                data.vsync = SettingsService.Instance.VSync ? 1 : 0;
                data.fieldOfView = SettingsService.Instance.FieldOfView;
                data.bindings = OutpostZero.Player.ControlBindings.Pack();
                data.shake = SettingsService.Instance.ScreenShake;
                data.volume = SettingsService.Instance.MasterVolume;
                data.textScale = SettingsService.Instance.TextScale;
                data.subtitles = SettingsService.Instance.Subtitles;
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
                    modules.Add(new ModuleSave { kind = module.kind, x = module.x, z = module.z, rotation = module.rotation, integrity = module.integrity <= 0 ? 100 : module.integrity });
                }
                data.modules = modules.ToArray();
            }
            return data;
        }

        public void Apply(SaveGameData data)
        {
            if (data == null) return;
            WorldClock.Instance?.Set(data.day, data.hour);
            ColonyStorage.Instance?.Set(data.colonyScrap, data.food, data.water);
            FactionTrade.Instance?.SetStanding(data.factionStanding);
            SettingsService.Instance?.ApplySnapshot(data.shake, data.volume, data.textScale, data.subtitles, data.language);
            SettingsService.Instance?.ApplyPresentation(data.sfxVolume, data.musicVolume, data.quality, data.vsync, data.fieldOfView, data.bindings);
            TutorialDirector.Instance?.SetFinished(data.tutorialDone);
            WorldMapService.Instance?.RestoreCleared(data.districtsCleared);
            if (data.districtIndex > data.districtsCleared) WorldMapService.Instance?.SelectIndex(data.districtIndex);
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
                        modules.Add(new PlacedModule { kind = module.kind, x = module.x, z = module.z, rotation = module.rotation, integrity = module.integrity <= 0 ? 100 : module.integrity });
                    }
                }
                GridBuilder.Instance.Restore(modules.ToArray());
            }
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.CampManagement);
        }
    }
}

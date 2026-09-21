using System;

namespace OutpostZero.Shell
{
    [Serializable]
    public class SurvivorSave
    {
        public string id;
        public string displayName;
        public string trait;
        public bool alive;
        public bool leader;
        public float morale;
        public float hunger;
        public float thirst;
        public int opinion;
        public int injury;
        public bool needsTracked;
        public string task;
        public string bond;
    }

    [Serializable]
    public class ModuleSave
    {
        public string kind;
        public float x;
        public float z;
        public int rotation;
        public int integrity = 100;
    }

    [Serializable]
    public class SaveGameData
    {
        public int schemaVersion = 1;
        public int day = 1;
        public float hour = 18.5f;
        public int colonyScrap;
        public int food;
        public int water;
        public int kills;
        public int districtsCleared;
        public int districtIndex;
        public float sfxVolume = 1f;
        public float musicVolume = 0.7f;
        public int quality = 1;
        public int vsync = 1;
        public float fieldOfView = 55f;
        public string bindings = "";
        public int factionStanding;
        public bool tutorialDone;
        public string codex = "";
        public string weaponMods = "";
        public string memorial = "";
        public string corpses = "";
        public int mercy;
        public string language = "en";
        public float shake = 1f;
        public float volume = 1f;
        public float textScale = 1f;
        public bool subtitles = true;
        public SurvivorSave[] survivors = Array.Empty<SurvivorSave>();
        public ModuleSave[] modules = Array.Empty<ModuleSave>();
    }

    public static class SaveCodec
    {
        public const int CurrentSchema = 1;

        public static string Serialize(SaveGameData data)
        {
            if (data == null) data = new SaveGameData();
            data.schemaVersion = CurrentSchema;
            return UnityEngine.JsonUtility.ToJson(data, true);
        }

        public static bool TryDeserialize(string json, out SaveGameData data, out string error)
        {
            data = null;
            error = null;
            if (string.IsNullOrEmpty(json))
            {
                error = "empty";
                return false;
            }
            try
            {
                data = UnityEngine.JsonUtility.FromJson<SaveGameData>(json);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            if (data == null || data.schemaVersion != CurrentSchema)
            {
                error = "schema";
                return false;
            }
            return true;
        }
    }
}

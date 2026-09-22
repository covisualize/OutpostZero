using System;
using System.IO;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Writes finished runs beside the save folder. The campaign file itself stays schema 1.
    /// </summary>
    public class RunArchive : MonoBehaviour
    {
        public static RunArchive Instance { get; private set; }

        private RunBoard.Run[] runs = new RunBoard.Run[0];

        public RunBoard.Run[] Runs => runs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Load();
        }

        public static void NoteCurrent(bool won)
        {
            Instance?.Note(won);
        }

        public void Note(bool won)
        {
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.LifetimeKills : 0;
            int lost = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Memorials.Count : 0;
            int cleared = WorldMapService.Instance != null ? WorldMapService.Instance.ClearedCount : 0;
            string name = "Leader";
            if (SurvivorRoster.Instance != null && SurvivorRoster.Instance.Leader != null && !string.IsNullOrEmpty(SurvivorRoster.Instance.Leader.displayName))
                name = SurvivorRoster.Instance.Leader.displayName;
            Remember(RunBoard.Make(name, day, kills, lost, cleared, won));
        }

        public void Remember(RunBoard.Run run)
        {
            runs = RunBoard.Insert(runs, run);
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "outpost-zero-runs.txt"), RunBoard.Pack(runs));
            }
            catch (Exception)
            {
                GameplayFeedback.Toast(FightSay.Ledger(null));
            }
        }

        private void Load()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "outpost-zero-runs.txt");
                if (!File.Exists(path))
                {
                    runs = new RunBoard.Run[0];
                    return;
                }
                runs = RunBoard.Unpack(File.ReadAllText(path));
            }
            catch (Exception)
            {
                runs = new RunBoard.Run[0];
            }
        }
    }
}

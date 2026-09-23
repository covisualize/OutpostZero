using System;
using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>A system that writes its own part of the save under an id that never changes between builds.</summary>
    public interface ISaveable
    {
        string SaveId { get; }
        string CaptureState();
        void RestoreState(string state);
    }

    [Serializable]
    public class SaveBlob
    {
        public string id = "";
        public string state = "";
    }

    /// <summary>
    /// Every live <see cref="ISaveable"/> keyed by its save id. A part loaded before its owner exists
    /// waits here and is handed over when the owner registers, and is written back unchanged meanwhile,
    /// so a save never loses a system's state because that system was not in the scene.
    /// </summary>
    public static class SaveRegistry
    {
        private static readonly Dictionary<string, ISaveable> live = new Dictionary<string, ISaveable>();
        private static readonly Dictionary<string, string> waiting = new Dictionary<string, string>();

        public static int Count => live.Count;

        public static bool Register(ISaveable part)
        {
            if (part == null || string.IsNullOrEmpty(part.SaveId)) return false;
            if (live.TryGetValue(part.SaveId, out var held) && !ReferenceEquals(held, part))
            {
                UnityEngine.Debug.LogWarning("[SaveRegistry] Two systems claim the save id " + part.SaveId);
                return false;
            }
            live[part.SaveId] = part;
            if (waiting.TryGetValue(part.SaveId, out string state))
            {
                waiting.Remove(part.SaveId);
                part.RestoreState(state);
            }
            return true;
        }

        public static void Unregister(ISaveable part)
        {
            if (part == null || string.IsNullOrEmpty(part.SaveId)) return;
            if (live.TryGetValue(part.SaveId, out var held) && ReferenceEquals(held, part)) live.Remove(part.SaveId);
        }

        public static SaveBlob[] Capture()
        {
            var ids = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in waiting) ids[pair.Key] = pair.Value;
            foreach (var pair in live) ids[pair.Key] = pair.Value.CaptureState() ?? "";
            var blobs = new SaveBlob[ids.Count];
            int i = 0;
            foreach (var pair in ids) blobs[i++] = new SaveBlob { id = pair.Key, state = pair.Value };
            return blobs;
        }

        public static void Restore(SaveBlob[] blobs)
        {
            waiting.Clear();
            var seen = new HashSet<string>();
            if (blobs != null)
            {
                foreach (var blob in blobs)
                {
                    if (blob == null || string.IsNullOrEmpty(blob.id) || !seen.Add(blob.id)) continue;
                    if (live.TryGetValue(blob.id, out var part)) part.RestoreState(blob.state ?? "");
                    else waiting[blob.id] = blob.state ?? "";
                }
            }
            foreach (var pair in live)
                if (!seen.Contains(pair.Key)) pair.Value.RestoreState("");
        }

        public static string Find(SaveBlob[] blobs, string id)
        {
            if (blobs == null) return null;
            foreach (var blob in blobs)
                if (blob != null && blob.id == id) return blob.state ?? "";
            return null;
        }

        public static void Clear()
        {
            live.Clear();
            waiting.Clear();
        }
    }
}

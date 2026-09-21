using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Remembers which hints and codex pages the player has earned, and shows a hint once.
    /// </summary>
    public class CodexDirector : MonoBehaviour
    {
        public static CodexDirector Instance { get; private set; }

        private string packed = "";

        public string Packed => packed ?? "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public static void Hear(string signal)
        {
            if (Instance != null) Instance.Note(signal);
            else TutorialDirector.Instance?.Note(signal);
        }

        public void Note(string signal)
        {
            if (CodexBook.TryHint(packed, signal, out string text, out string next))
            {
                packed = next;
                GameplayFeedback.Toast(text);
            }
            TutorialDirector.Instance?.Note(signal);
        }

        public void Unlock(string id)
        {
            packed = CodexBook.Remember(packed, id, out bool added);
            if (!added) return;
            for (int i = 0; i < CodexBook.Entries.Length; i++)
            {
                if (CodexBook.Entries[i].Id != id) continue;
                GameplayFeedback.Toast("Codex: " + CodexBook.Entries[i].Title);
                return;
            }
        }

        public void Restore(string value)
        {
            packed = value ?? "";
        }
    }
}

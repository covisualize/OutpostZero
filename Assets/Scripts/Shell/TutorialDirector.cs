using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Ordered gates for the first expedition. A signal only advances the step it matches.
    /// </summary>
    public static class TutorialTrack
    {
        public static readonly string[] Steps =
        {
            "WASD move, mouse aim, left click fire. C crouch, Shift sprint.",
            "Fire the weapon in your hands. R reloads. 1-4 swaps.",
            "Crouch to cut your exposure. Noise still draws the horde.",
            "Kill and scavenge, then extract at the sanctuary gate.",
            "Tab opens the pack. If you fall, the next survivor takes the gate."
        };

        public static readonly string[] Gates = { "move", "fire", "crouch", "loot", "pack" };

        public static int Advance(int index, string signal, out bool finished)
        {
            if (index >= Gates.Length)
            {
                finished = true;
                return index;
            }
            if (signal != Gates[index])
            {
                finished = false;
                return index;
            }
            int next = index + 1;
            finished = next >= Gates.Length;
            return next;
        }
    }

    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        [SerializeField] private int index;
        [SerializeField] private bool finished;
        private float nextStepAt;

        public bool Finished => finished;
        public string Current => finished || index >= TutorialTrack.Steps.Length ? string.Empty : TutorialTrack.Steps[index];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            nextStepAt = Time.unscaledTime + 18f;
        }

        private void Update()
        {
            if (finished) return;
            if (Time.unscaledTime < nextStepAt) return;
            index++;
            nextStepAt = Time.unscaledTime + 18f;
            if (index >= TutorialTrack.Steps.Length) finished = true;
        }

        public void Note(string signal)
        {
            if (finished) return;
            index = TutorialTrack.Advance(index, signal, out bool nowFinished);
            if (nowFinished) finished = true;
            else nextStepAt = Time.unscaledTime + 18f;
        }

        public void Dismiss() => finished = true;

        public void SetFinished(bool value)
        {
            finished = value;
            if (value) index = TutorialTrack.Steps.Length;
        }
    }
}

using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        private static readonly string[] steps =
        {
            "WASD move, mouse aim, left click fire. C crouch, Shift sprint.",
            "Q medkit. R reload. 1-4 swap weapons. G throws a lure.",
            "Noise draws the horde. Crouch and kill the lights to stay hidden.",
            "Kill 8, scavenge 15 scrap, then extract at the sanctuary gate.",
            "If you fall, the next survivor takes the gate. Tab opens the pack."
        };

        [SerializeField] private int index;
        [SerializeField] private bool finished;
        private float nextStepAt;

        public bool Finished => finished;
        public string Current => finished || index >= steps.Length ? string.Empty : steps[index];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            nextStepAt = Time.unscaledTime + 7f;
        }

        private void Update()
        {
            if (finished) return;
            if (Time.unscaledTime < nextStepAt) return;
            index++;
            nextStepAt = Time.unscaledTime + 7f;
            if (index >= steps.Length) finished = true;
        }

        public void Dismiss() => finished = true;

        public void SetFinished(bool value)
        {
            finished = value;
            if (value) index = steps.Length;
        }
    }
}

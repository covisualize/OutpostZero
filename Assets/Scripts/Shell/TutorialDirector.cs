using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>
    /// One guided step: its string-table key, the English fallback, and the signal that clears it.
    /// A step gated on <see cref="TutorialTrack.Read"/> clears when the player presses Next.
    /// </summary>
    public struct TutorialStep
    {
        public string Key;
        public string Fallback;
        public string Gate;

        public TutorialStep(string key, string fallback, string gate)
        {
            Key = key;
            Fallback = fallback;
            Gate = gate;
        }
    }

    /// <summary>
    /// Ordered gates for Day 1 in the sanctuary and for the first expedition.
    /// A signal only advances the step it matches.
    /// </summary>
    public static class TutorialTrack
    {
        public const string Read = "next";
        public const string CampDone = "tut.day1";

        public static readonly TutorialStep[] Camp =
        {
            new TutorialStep("day1.0", "Pick a survivor and give them a task.", "assign"),
            new TutorialStep("day1.1", "The stores line shows scrap, food and water. When they run dry, the sanctuary starves.", Read),
            new TutorialStep("day1.2", "Press {key:Build}, pick Barricade and place it on the yard.", "barricade"),
            new TutorialStep("day1.3", "Craft a bandage at the bench. It costs one scrap.", "craft_bandage"),
            new TutorialStep("day1.4", "Send the first expedition out through the gate.", "launch"),
        };

        public static readonly string[] Steps =
        {
            "WASD move, mouse aim, left click fire. {key:Crouch} crouch, {key:Sprint} sprint.",
            "Fire the weapon in your hands. {key:Reload} reloads. 1-4 swaps.",
            "Crouch to cut your exposure. Noise still draws the horde.",
            "Kill and scavenge, then extract at the sanctuary gate.",
            "{key:Inventory} opens the pack. If you fall, the next survivor takes the gate."
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

        public static int Advance(TutorialStep[] steps, int index, string signal, out bool finished)
        {
            int count = steps != null ? steps.Length : 0;
            if (index >= count)
            {
                finished = true;
                return index;
            }
            if (signal != steps[index].Gate)
            {
                finished = false;
                return index;
            }
            int next = index + 1;
            finished = next >= count;
            return next;
        }

        public static bool InCamp(GameState state) => state == GameState.CampManagement;
    }

    public class TutorialDirector : MonoBehaviour
    {
        public static TutorialDirector Instance { get; private set; }

        [SerializeField] private int index;
        [SerializeField] private bool finished;
        [SerializeField] private int campIndex;
        [SerializeField] private bool campFinished;

        public bool Finished => finished;
        public bool CampFinished => campFinished;
        public int CampIndex => campIndex;

        public string Current
        {
            get
            {
                var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
                if (TutorialTrack.InCamp(state)) return CampCurrent;
                return finished || index >= TutorialTrack.Steps.Length ? string.Empty : Loc.Lesson(index, TutorialTrack.Steps[index]);
            }
        }

        public string CampCurrent
        {
            get
            {
                if (campFinished || campIndex >= TutorialTrack.Camp.Length) return string.Empty;
                var step = TutorialTrack.Camp[campIndex];
                return Loc.Hint(step.Key, step.Fallback);
            }
        }

        public bool CampAwaitsRead => !campFinished && campIndex < TutorialTrack.Camp.Length && TutorialTrack.Camp[campIndex].Gate == TutorialTrack.Read;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Note(string signal)
        {
            if (!campFinished)
            {
                campIndex = TutorialTrack.Advance(TutorialTrack.Camp, campIndex, signal, out bool campNow);
                if (campNow) FinishCamp();
            }
            if (finished) return;
            index = TutorialTrack.Advance(index, signal, out bool nowFinished);
            if (nowFinished) finished = true;
        }

        public void Dismiss()
        {
            finished = true;
            FinishCamp();
        }

        public void SetFinished(bool value)
        {
            finished = value;
            if (value) index = TutorialTrack.Steps.Length;
            else index = 0;
        }

        public void RestoreCamp(bool done)
        {
            campFinished = done;
            campIndex = done ? TutorialTrack.Camp.Length : 0;
        }

        private void FinishCamp()
        {
            campFinished = true;
            campIndex = TutorialTrack.Camp.Length;
            CodexDirector.Instance?.Unlock(TutorialTrack.CampDone);
        }
    }
}

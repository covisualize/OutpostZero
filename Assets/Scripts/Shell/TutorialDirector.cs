using System.Collections.Generic;
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
        public string Mark;

        public TutorialStep(string key, string fallback, string gate, string mark = "")
        {
            Key = key;
            Fallback = fallback;
            Gate = gate;
            Mark = mark ?? "";
        }
    }

    /// <summary>
    /// Which camp control a tutorial step points at. The panel rings the marked control and dims
    /// the rows that hold nothing marked, so the step reads as "press this".
    /// </summary>
    public static class TutorialMark
    {
        public const string Task = "task";
        public const string Stores = "stores";
        public const string Barricade = "barricade";
        public const string Bandage = "bandage";
        public const string Leave = "leave";
        public const float Dim = 0.4f;
        public const float Ring = 2f;

        public static bool Lit(string current, string tag) => !string.IsNullOrEmpty(current) && current == tag;

        public static float Opacity(bool anyLit, bool holdsLit, bool guide) => !anyLit || holdsLit || guide ? 1f : Dim;
    }

    /// <summary>
    /// Ordered gates for Day 1 in the sanctuary and for the first expedition.
    /// A signal only advances the step it matches. <see cref="TutorialBook.Ensure"/> fills both tracks from
    /// Resources/TutorialBook; until then, and when the book leaves a track empty, the built-in steps answer.
    /// </summary>
    public static class TutorialTrack
    {
        public const string Read = "next";
        public const string CampDone = "tut.day1";
        public const string CampTrack = "camp";
        public const string StreetTrack = "street";

        public static readonly TutorialStep[] CodeCamp =
        {
            new TutorialStep("day1.0", "Pick a survivor and give them a task.", "assign", TutorialMark.Task),
            new TutorialStep("day1.1", "The stores line shows scrap, food and water. When they run dry, the sanctuary starves.", Read, TutorialMark.Stores),
            new TutorialStep("day1.2", "Press {key:Build}, pick Barricade and place it on the yard.", "barricade", TutorialMark.Barricade),
            new TutorialStep("day1.3", "Craft a bandage at the bench. It costs one scrap.", "craft_bandage", TutorialMark.Bandage),
            new TutorialStep("day1.4", "Send the first expedition out through the gate.", "launch", TutorialMark.Leave),
        };

        public static readonly TutorialStep[] CodeStreet =
        {
            new TutorialStep("lesson.0", "WASD move, mouse aim, left click fire. {key:Crouch} crouch, {key:Sprint} sprint.", "move"),
            new TutorialStep("lesson.1", "Fire the weapon in your hands. {key:Reload} reloads. 1-4 swaps.", "fire"),
            new TutorialStep("lesson.2", "Crouch to cut your exposure. Noise still draws the horde.", "crouch"),
            new TutorialStep("lesson.3", "Kill and scavenge, then extract at the sanctuary gate.", "loot"),
            new TutorialStep("lesson.4", "{key:Inventory} opens the pack. If you fall, the next survivor takes the gate.", "pack")
        };

        private static TutorialStep[] bookCamp;
        private static TutorialStep[] bookStreet;

        public static TutorialStep[] Camp => bookCamp ?? CodeCamp;
        public static TutorialStep[] Street => bookStreet ?? CodeStreet;
        public static bool FromAsset => bookCamp != null || bookStreet != null;

        public static string[] Steps => Pluck(Street, false);
        public static string[] Gates => Pluck(Street, true);

        public static void Use(IList<TutorialStep> camp, IList<TutorialStep> street)
        {
            bookCamp = Valid(camp);
            bookStreet = Valid(street);
        }

        public static void Clear()
        {
            bookCamp = null;
            bookStreet = null;
        }

        private static TutorialStep[] Valid(IList<TutorialStep> steps)
        {
            if (steps == null) return null;
            var kept = new List<TutorialStep>();
            foreach (var step in steps)
                if (!string.IsNullOrEmpty(step.Key) && !string.IsNullOrEmpty(step.Gate)) kept.Add(step);
            return kept.Count > 0 ? kept.ToArray() : null;
        }

        private static string[] Pluck(TutorialStep[] steps, bool gates)
        {
            var values = new string[steps.Length];
            for (int i = 0; i < steps.Length; i++) values[i] = gates ? steps[i].Gate : steps[i].Fallback;
            return values;
        }

        public static int Advance(int index, string signal, out bool finished)
        {
            return Advance(Street, index, signal, out finished);
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
        public int Index => index;

        public string Current
        {
            get
            {
                var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.ExpeditionActive;
                if (TutorialTrack.InCamp(state)) return CampCurrent;
                var street = TutorialTrack.Street;
                return finished || index >= street.Length ? string.Empty : Loc.Hint(street[index].Key, street[index].Fallback);
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

        public string CampMark => campFinished || campIndex >= TutorialTrack.Camp.Length ? string.Empty : TutorialTrack.Camp[campIndex].Mark;

        public bool CampAwaitsRead => !campFinished && campIndex < TutorialTrack.Camp.Length && TutorialTrack.Camp[campIndex].Gate == TutorialTrack.Read;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            TutorialBook.Ensure();
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
            if (value) index = TutorialTrack.Street.Length;
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

using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The Day 1 and first-expedition steps, in order, loaded from Resources.
    /// Tools > Outpost Zero > Sync Tutorial Book rebuilds it.
    /// </summary>
    public class TutorialBook : ScriptableObject
    {
        public const string ResourcePath = "TutorialBook";

        public TutorialStepDefinition[] camp = new TutorialStepDefinition[0];
        public TutorialStepDefinition[] street = new TutorialStepDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<TutorialBook>(ResourcePath));
        }

        public static void Use(TutorialBook book)
        {
            if (book == null)
            {
                TutorialTrack.Clear();
                return;
            }
            TutorialTrack.Use(Steps(book.camp), Steps(book.street));
        }

        private static List<TutorialStep> Steps(TutorialStepDefinition[] list)
        {
            var steps = new List<TutorialStep>();
            if (list == null) return steps;
            foreach (var step in list)
                if (step != null) steps.Add(step.ToStep());
            return steps;
        }
    }
}

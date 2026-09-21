using System;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    public enum FlowStep
    {
        Boot,
        MainMenu,
        Sanctuary,
        Expedition,
        Results
    }

    /// <summary>
    /// Names, tips, and progress beats for Boot, the menu, camp, the street, and the extract card.
    /// The prototype still lives in one scene; the beats are what the loading card plays.
    /// </summary>
    public static class SceneRoute
    {
        public const string Version = "0.5.0";

        public static readonly string[] Tips =
        {
            "Noise carries farther than the shot that made it.",
            "Crouch before you fire. The street hears you stand.",
            "A dark block still has eyes past the lamplight.",
            "Extract at the sanctuary gate. The bag does not walk home.",
            "Barricades fail when nobody is posted on Guard.",
            "Cooked food lifts morale. A skipped meal does not.",
            "The generator drinks fuel once dusk settles.",
            "Three quiet districts are enough to hold the gate.",
            "Reload before the magazine clicks empty.",
            "Oil, toxic, and powder barrels chain if you break them.",
            "A medic on the board closes wounds between watches.",
            "Save before you leave the gate."
        };

        public static GameState StateFor(FlowStep step)
        {
            switch (step)
            {
                case FlowStep.Sanctuary: return GameState.CampManagement;
                case FlowStep.Expedition: return GameState.ExpeditionActive;
                case FlowStep.Results: return GameState.ExpeditionResults;
                default: return GameState.MainMenu;
            }
        }

        public static string Title(FlowStep step)
        {
            switch (step)
            {
                case FlowStep.Boot: return "WAKING THE GATE";
                case FlowStep.Sanctuary: return "THE SANCTUARY";
                case FlowStep.Expedition: return "INTO THE DISTRICT";
                case FlowStep.Results: return "BACK INSIDE";
                default: return "OUTPOST ZERO";
            }
        }

        public static string Tip(FlowStep step, int index)
        {
            int start = (int)step * 3;
            int slot = start + Math.Abs(index);
            return Tips[slot % Tips.Length];
        }

        public static float[] Beats(FlowStep step)
        {
            switch (step)
            {
                case FlowStep.Expedition: return new[] { 0.2f, 0.55f, 0.85f, 1f };
                case FlowStep.Sanctuary: return new[] { 0.35f, 0.7f, 1f };
                case FlowStep.Results: return new[] { 0.4f, 1f };
                default: return new[] { 0.45f, 1f };
            }
        }
    }
}

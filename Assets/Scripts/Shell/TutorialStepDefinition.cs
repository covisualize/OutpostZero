using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>One guided tutorial step, authored as data and listed in <see cref="TutorialBook"/>.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Tutorial Step", fileName = "TutorialStep")]
    public class TutorialStepDefinition : ScriptableObject
    {
        [Tooltip("\"camp\" for the Day 1 sanctuary track, \"street\" for the first expedition.")]
        public string track = TutorialTrack.CampTrack;

        [Tooltip("String-table key for the line shown while this step is open.")]
        public string key = "";

        [Tooltip("English line used when the string table lacks the key. {key:Action} names the bound key.")]
        [TextArea] public string fallback = "";

        [Tooltip("Signal that clears the step, such as assign, barricade or move. \"next\" waits for the Next button.")]
        public string gate = "";

        [Tooltip("Camp control the panel rings while the step is open: task, stores, barricade, bandage or leave.")]
        public string mark = "";

        public TutorialStep ToStep()
        {
            return new TutorialStep(key ?? "", fallback ?? "", gate ?? "", mark ?? "");
        }

        public void CopyFrom(string trackName, TutorialStep step)
        {
            track = trackName;
            key = step.Key;
            fallback = step.Fallback;
            gate = step.Gate;
            mark = step.Mark;
        }
    }
}

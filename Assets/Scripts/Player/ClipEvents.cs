using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>
    /// Adds footfalls, the reload finish and the attack contact to imported clips at runtime,
    /// once per shared clip, so the FBX import needs no hand-placed events.
    /// </summary>
    public static class ClipEvents
    {
        /// <summary>Arms every clip in <paramref name="rig"/>; returns true if any clip carries <paramref name="wanted"/>.</summary>
        public static bool Arm(RuntimeAnimatorController rig, string wanted = null)
        {
            bool found = false;
            if (rig == null) return false;
            foreach (var clip in rig.animationClips)
            {
                if (clip == null) continue;
                float[] marks = AimRig.EventsFor(Bare(clip.name), out string function);
                if (marks.Length == 0) continue;
                if (function == wanted) found = true;
                if (Carries(clip, function)) continue;
                for (int i = 0; i < marks.Length; i++)
                    clip.AddEvent(new AnimationEvent { time = clip.length * marks[i], functionName = function });
            }
            return found;
        }

        public static string Bare(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }

        private static bool Carries(AnimationClip clip, string function)
        {
            var events = clip.events;
            for (int i = 0; i < events.Length; i++)
                if (events[i].functionName == function) return true;
            return false;
        }
    }
}

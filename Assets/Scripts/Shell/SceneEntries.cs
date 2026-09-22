using System;
using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>Something that sets itself up when the flow arrives at a step, and stands down when it leaves.</summary>
    public interface ISceneEntry
    {
        void OnEnter(FlowStep step, FlowStep from);
        void OnExit(FlowStep step, FlowStep to);
    }

    public static class SceneEntries
    {
        private static readonly List<ISceneEntry> entries = new List<ISceneEntry>();

        public static int Count => entries.Count;

        public static void Register(ISceneEntry entry)
        {
            if (entry != null && !entries.Contains(entry)) entries.Add(entry);
        }

        public static void Unregister(ISceneEntry entry) => entries.Remove(entry);

        public static void Clear() => entries.Clear();

        /// <summary>Exit hooks run for the step being left, then enter hooks for the new one. One failing hook never blocks the rest.</summary>
        public static int Dispatch(FlowStep from, FlowStep to, Action<Exception> failed = null)
        {
            var snapshot = entries.ToArray();
            int reached = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i].OnExit(from, to); }
                catch (Exception e) { failed?.Invoke(e); }
            }
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].OnEnter(to, from);
                    reached++;
                }
                catch (Exception e) { failed?.Invoke(e); }
            }
            return reached;
        }
    }
}

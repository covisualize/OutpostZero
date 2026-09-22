using System;
using System.Collections.Generic;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>One hop of the scene flow. Handled means the caller already set up the step with its own context (a seed, a save slot).</summary>
    public readonly struct FlowContext
    {
        public readonly FlowStep From;
        public readonly FlowStep To;
        public readonly bool Handled;

        public FlowContext(FlowStep from, FlowStep to, bool handled)
        {
            From = from;
            To = to;
            Handled = handled;
        }
    }

    /// <summary>Something that sets itself up when the flow arrives at a step, and stands down when it leaves.</summary>
    public interface ISceneEntry
    {
        void OnEnter(FlowContext context);
        void OnExit(FlowContext context);
    }

    public enum ArrivalAction
    {
        None,
        Menu,
        Camp,
        Street
    }

    public static class FlowArrival
    {
        public static bool Frozen(GameState state) =>
            state == GameState.Paused
            || state == GameState.SuccessionScreen
            || state == GameState.GameOver
            || state == GameState.MainMenu
            || state == GameState.ExpeditionResults
            || state == GameState.Victory;

        /// <summary>What the game state has to do once the flow lands, so a bare Travel(step) is enough for the plain hops.</summary>
        public static ArrivalAction For(FlowContext context, GameState state)
        {
            if (context.Handled) return ArrivalAction.None;
            switch (context.To)
            {
                case FlowStep.MainMenu:
                    return state == GameState.MainMenu ? ArrivalAction.None : ArrivalAction.Menu;
                case FlowStep.Sanctuary:
                    return state == GameState.CampManagement || state == GameState.RaidActive ? ArrivalAction.None : ArrivalAction.Camp;
                case FlowStep.Expedition:
                    return state == GameState.ExpeditionActive ? ArrivalAction.None : ArrivalAction.Street;
                default:
                    return ArrivalAction.None;
            }
        }
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
        public static int Dispatch(FlowContext context, Action<Exception> failed = null)
        {
            var snapshot = entries.ToArray();
            int reached = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i].OnExit(context); }
                catch (Exception e) { failed?.Invoke(e); }
            }
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].OnEnter(context);
                    reached++;
                }
                catch (Exception e) { failed?.Invoke(e); }
            }
            return reached;
        }
    }
}

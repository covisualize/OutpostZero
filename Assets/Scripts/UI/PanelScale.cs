using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using OutpostZero.Core;

namespace OutpostZero.UI
{
    /// <summary>
    /// Every UI Toolkit panel the game builds, so the interface-size setting scales them together.
    /// </summary>
    public static class PanelScale
    {
        private static readonly List<PanelSettings> panels = new List<PanelSettings>();

        public static float Current { get; private set; } = 1f;

        public static void Track(PanelSettings panel)
        {
            if (panel == null) return;
            if (!panels.Contains(panel)) panels.Add(panel);
            panel.scale = Current;
        }

        public static void Apply(float stored)
        {
            float scale = PlayOptions.UiScale(stored);
            if (Mathf.Approximately(scale, Current)) return;
            Current = scale;
            for (int i = panels.Count - 1; i >= 0; i--)
            {
                if (panels[i] == null) panels.RemoveAt(i);
                else panels[i].scale = scale;
            }
        }
    }
}

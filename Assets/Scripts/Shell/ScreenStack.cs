using System.Collections.Generic;

namespace OutpostZero.Shell
{
    public enum MenuScreen
    {
        None,
        Codex,
        CodexEntry,
        Credits,
        Saves,
        NewGame
    }

    public enum BackAction
    {
        None,
        CloseSettings,
        CloseTrade,
        Pop,
        TogglePause
    }

    /// <summary>
    /// Sub-screens layered over the main or pause menu. Back always peels the top one off first,
    /// so Escape never skips past a screen the player can see.
    /// </summary>
    public sealed class ScreenStack
    {
        private readonly List<MenuScreen> screens = new List<MenuScreen>();

        public int Depth => screens.Count;
        public MenuScreen Top => screens.Count > 0 ? screens[screens.Count - 1] : MenuScreen.None;

        public void Push(MenuScreen screen)
        {
            if (screen == MenuScreen.None || Top == screen) return;
            screens.Add(screen);
        }

        public MenuScreen Pop()
        {
            if (screens.Count == 0) return MenuScreen.None;
            var top = screens[screens.Count - 1];
            screens.RemoveAt(screens.Count - 1);
            return top;
        }

        public bool Contains(MenuScreen screen) => screens.Contains(screen);

        public void Clear() => screens.Clear();

        public string Signature()
        {
            if (screens.Count == 0) return "";
            var parts = new string[screens.Count];
            for (int i = 0; i < screens.Count; i++) parts[i] = screens[i].ToString();
            return string.Join(">", parts);
        }
    }

    public static class BackRoute
    {
        /// <summary>What the Pause/Back input does given what is on screen, innermost first.</summary>
        public static BackAction For(bool settingsOpen, bool tradeOpen, int depth, bool pausable)
        {
            if (settingsOpen) return BackAction.CloseSettings;
            if (tradeOpen) return BackAction.CloseTrade;
            if (depth > 0) return BackAction.Pop;
            return pausable ? BackAction.TogglePause : BackAction.None;
        }
    }
}

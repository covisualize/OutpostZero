namespace OutpostZero.Core
{
    public enum SettingsClose
    {
        Close,
        Ask,
        StayOpen
    }

    /// <summary>
    /// The settings panel's keep-or-revert check, and the focus mute.
    /// </summary>
    public static class SettingsDraft
    {
        /// <summary>
        /// Closing with changes asks first. Backing out of the question returns to the panel.
        /// </summary>
        public static SettingsClose OnClose(bool dirty, bool asking)
        {
            if (!dirty) return SettingsClose.Close;
            return asking ? SettingsClose.StayOpen : SettingsClose.Ask;
        }

        public static bool Dirty(string opened, string now)
        {
            if (string.IsNullOrEmpty(opened)) return false;
            return opened != now;
        }

        public static float Heard(float master, bool focused)
        {
            if (!focused) return 0f;
            return master < 0f ? 0f : master > 1f ? 1f : master;
        }
    }
}

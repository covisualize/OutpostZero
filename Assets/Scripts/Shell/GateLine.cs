namespace OutpostZero.Shell
{
    /// <summary>
    /// Crate, gate, road, and save lines follow the language.
    /// English keeps Pack is too heavy, Extracted, and Game saved.
    /// </summary>
    public static class GateLine
    {
        public static string Heavy(string language) => Word("camp.heavy", language);
        public static string Left(string language) => Word("loot.left", language);
        public static string Open(string language) => Word("loot.open", language);
        public static string Empty(string language) => Word("loot.empty", language);
        public static string District(string language) => Word("gate.open", language);
        public static string Drag(string language) => Word("gate.drag", language);
        public static string Quota(string language) => Word("gate.quota", language);
        public static string Close(string language) => Word("gate.close", language);
        public static string Road(string language) => Word("gate.road", language);
        public static string Radio(string language) => Word("gate.radio", language);
        public static string Broadcast(string language) => Word("gate.out", language);
        public static string Extracted(string language) => Word("gate.extracted", language);
        public static string SaveFail(string language) => Word("save.fail", language);
        public static string Saved(string language) => Word("save.ok", language);
        public static string CampOnly(string language) => Word("save.camp_only", language);
        public static string NoFile(string language) => Word("save.none", language);
        public static string Unread(string language) => Word("save.bad", language);
        public static string Loaded(string language) => Word("save.loaded", language);
        public static string AutoLoaded(string language) => Word("save.auto", language);

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }
    }
}

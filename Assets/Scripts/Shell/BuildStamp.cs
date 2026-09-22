using System.Text.RegularExpressions;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The version string the menu and credits show, read from Resources/version.json.
    /// Falls back to SceneRoute.Version when the file is missing or unreadable.
    /// </summary>
    public static class BuildStamp
    {
        private static readonly Regex Field = new Regex("\"version\"\\s*:\\s*\"([^\"]+)\"");
        private static string cached;

        public static string Version
        {
            get
            {
                if (cached != null) return cached;
                var asset = Resources.Load<TextAsset>("version");
                cached = Parse(asset != null ? asset.text : null);
                return cached;
            }
        }

        public static string Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return SceneRoute.Version;
            var match = Field.Match(json);
            return match.Success ? match.Groups[1].Value : SceneRoute.Version;
        }
    }
}

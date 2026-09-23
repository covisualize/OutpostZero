using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The version string the menu and credits show. A player build reads the StreamingAssets/version.json
    /// that BuildScript writes (tag plus commit). The editor and older builds read Resources/version.json,
    /// then fall back to SceneRoute.Version when neither file is readable.
    /// </summary>
    public static class BuildStamp
    {
        public const string FileName = "version.json";

        private static readonly Regex Field = new Regex("\"version\"\\s*:\\s*\"([^\"]+)\"");
        private static readonly Regex CommitField = new Regex("\"commit\"\\s*:\\s*\"([^\"]*)\"");
        private static string cached;
        private static string cachedCommit;

        public static string Version
        {
            get
            {
                if (cached == null) Load();
                return cached;
            }
        }

        public static string Commit
        {
            get
            {
                if (cached == null) Load();
                return cachedCommit;
            }
        }

        private static void Load()
        {
            string text = Streamed();
            if (text == null)
            {
                var asset = Resources.Load<TextAsset>("version");
                text = asset != null ? asset.text : null;
            }
            cached = Parse(text);
            cachedCommit = ParseCommit(text);
        }

        private static string Streamed()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, FileName);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static string Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return SceneRoute.Version;
            var match = Field.Match(json);
            return match.Success ? match.Groups[1].Value : SceneRoute.Version;
        }

        public static string ParseCommit(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";
            var match = CommitField.Match(json);
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}

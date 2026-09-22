#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Shell;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Writes Localization/strings.csv beside Assets so translators work on one file per release.
    /// </summary>
    public static class StringTableExport
    {
        public const string OutputPath = "Localization/strings.csv";

        [MenuItem("Tools/Outpost Zero/Export Strings CSV", false, 4)]
        public static void ExportFromMenu()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, LocCsv.Export(), new System.Text.UTF8Encoding(false));
            foreach (var language in LocCsv.Languages)
            {
                var missing = Loc.MissingIn(language);
                if (missing.Count > 0) Debug.LogWarning("[StringTableExport] " + language + " is missing " + missing.Count + " keys, first " + missing[0]);
            }
            Debug.Log("[StringTableExport] Wrote " + OutputPath);
        }
    }
}
#endif

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Loads translator sheets from StreamingAssets/Localization/*.csv into <see cref="Loc"/>, so a new
    /// language ships as a file with no rebuild. Files load in name order; a later file replaces a code
    /// an earlier one defined.
    /// </summary>
    public static class LocPacks
    {
        public const string Folder = "Localization";

        public static int Load(string directory, List<string> problems)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return 0;
            var files = new List<string>(Directory.GetFiles(directory, "*.csv"));
            files.Sort(System.StringComparer.Ordinal);
            int added = 0;
            foreach (var file in files)
            {
                string name = Path.GetFileName(file);
                var found = new List<string>();
                List<LocCsv.Pack> packs;
                try
                {
                    packs = LocCsv.Import(File.ReadAllText(file, Encoding.UTF8), found);
                }
                catch (IOException exception)
                {
                    problems?.Add(name + ": " + exception.Message);
                    continue;
                }
                foreach (var line in found) problems?.Add(name + ": " + line);
                added += Register(packs);
            }
            return added;
        }

        public static int Register(List<LocCsv.Pack> packs)
        {
            int added = 0;
            if (packs == null) return 0;
            foreach (var pack in packs)
            {
                if (pack.Lines.Count == 0) continue;
                Loc.AddPack(pack.Code, pack.Name, new Dictionary<string, string>(pack.Lines));
                added++;
            }
            return added;
        }
    }
}

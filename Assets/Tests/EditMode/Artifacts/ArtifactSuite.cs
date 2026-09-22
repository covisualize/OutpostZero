using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OutpostZero.Tests.EditMode.Artifacts
{
    /// <summary>
    /// Reads the committed pipeline output as plain files so a missing texture, a renamed FBX or a
    /// pose-sized collider fails the typecheck job as well as the Unity job. Every check returns
    /// one line per problem naming the file, so the CI log says what to rebuild.
    /// </summary>
    public static class ArtifactSuite
    {
        public const float SizeTolerance = 0.1f;
        public const float CenterTolerance = 0.05f;
        public const double MaxTiltDegrees = 45d;
        public const double FloorTolerance = 0.05d;
        public const string RootTransform = "-8679921383154817045";
        private const string Vector = @"\{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)\}";

        public class Entry
        {
            public string Id;
            public string Category;
            public string Output;
            public List<string> Tags;
            public string Stem => Output.Substring(0, Output.Length - 4);
            public string Fbx => "Assets/Models/" + Output;
            public string Prefab => "Assets/Prefabs/" + Stem + ".prefab";
            public string Sidecar => "Assets/Models/" + Stem + ".meta.json";
        }

        public class Manifest
        {
            public List<Entry> Entries = new List<Entry>();
            public List<string> Textures = new List<string>();
            public int TextureSize;
        }

        public static Manifest LoadManifest(string root)
        {
            var node = MiniJson.Object(MiniJson.Parse(File.ReadAllText(Path.Combine(root, "BlenderScripts", "assets.manifest.json"))));
            var manifest = new Manifest
            {
                Textures = MiniJson.Strings(node, "textures"),
                TextureSize = (int)MiniJson.Num(node, "textureSize"),
            };
            foreach (var item in MiniJson.List(node["entries"]))
            {
                var entry = MiniJson.Object(item);
                manifest.Entries.Add(new Entry
                {
                    Id = MiniJson.Str(entry, "id"),
                    Category = MiniJson.Str(entry, "category"),
                    Output = MiniJson.Str(entry, "output"),
                    Tags = MiniJson.Strings(entry, "tags"),
                });
            }
            return manifest;
        }

        public static Dictionary<string, object> Sidecar(string root, Entry entry)
        {
            string path = Path.Combine(root, entry.Sidecar);
            return File.Exists(path) ? MiniJson.Object(MiniJson.Parse(File.ReadAllText(path))) : null;
        }

        public static List<string> ManifestMatchesDisk(string root, Manifest manifest)
        {
            var problems = new List<string>();
            var listed = new HashSet<string>(manifest.Entries.Select(e => e.Fbx));
            foreach (var entry in manifest.Entries)
            {
                if (!File.Exists(Path.Combine(root, entry.Fbx))) problems.Add(entry.Fbx + " is in the manifest but missing on disk");
                else if (!File.Exists(Path.Combine(root, entry.Fbx + ".meta"))) problems.Add(entry.Fbx + ".meta is missing");
                var sidecar = Sidecar(root, entry);
                if (sidecar == null)
                {
                    problems.Add(entry.Sidecar + " is missing");
                    continue;
                }
                if (MiniJson.Str(sidecar, "id") != entry.Id) problems.Add(entry.Sidecar + " names " + MiniJson.Str(sidecar, "id") + " not " + entry.Id);
                if (MiniJson.Str(sidecar, "category") != entry.Category) problems.Add(entry.Sidecar + " category " + MiniJson.Str(sidecar, "category"));
                if (!File.Exists(Path.Combine(root, entry.Sidecar + ".meta"))) problems.Add(entry.Sidecar + ".meta is missing");
            }
            string models = Path.Combine(root, "Assets", "Models");
            if (Directory.Exists(models))
            {
                foreach (var file in Directory.GetFiles(models, "*.fbx", SearchOption.AllDirectories))
                {
                    string relative = Relative(root, file);
                    if (!listed.Contains(relative)) problems.Add(relative + " is on disk but not in the manifest");
                }
            }
            return problems;
        }

        public static List<string> UvsAndTextures(string root, Manifest manifest)
        {
            var problems = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                string fbx = Path.Combine(root, entry.Fbx);
                if (File.Exists(fbx) && !Contains(File.ReadAllBytes(fbx), "LayerElementUV")) problems.Add(entry.Fbx + " has no UVs");
                foreach (var suffix in manifest.Textures)
                {
                    string map = "Assets/Models/" + entry.Stem + "_" + suffix + ".png";
                    string path = Path.Combine(root, map);
                    if (!File.Exists(path))
                    {
                        problems.Add(map + " is missing");
                        continue;
                    }
                    if (!File.Exists(path + ".meta")) problems.Add(map + ".meta is missing");
                    if (suffix == "Icon") continue;
                    if (!PngSize(path, out int width, out int height)) problems.Add(map + " is not a PNG");
                    else if (width != manifest.TextureSize || height != manifest.TextureSize) problems.Add(map + " is " + width + "x" + height);
                }
            }
            return problems;
        }

        public static List<string> PrefabExistsForEveryModel(string root, Manifest manifest)
        {
            var problems = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                string path = Path.Combine(root, entry.Prefab);
                if (!File.Exists(path))
                {
                    problems.Add(entry.Prefab + " is missing");
                    continue;
                }
                if (!File.Exists(path + ".meta")) problems.Add(entry.Prefab + ".meta is missing");
                string text = File.ReadAllText(path);
                string fbxGuid = Guid(Path.Combine(root, entry.Fbx + ".meta"));
                var source = Regex.Match(text, @"m_SourcePrefab: \{fileID: 100100000, guid: (\w+)");
                if (!source.Success || source.Groups[1].Value != fbxGuid) problems.Add(entry.Prefab + " does not come from " + entry.Fbx);
                var sidecar = Sidecar(root, entry);
                string collider = sidecar != null ? MiniJson.Str(sidecar, "collider") : "box";
                if (collider != "none" && !Regex.IsMatch(text, @"^(BoxCollider|MeshCollider|CapsuleCollider|SphereCollider):", RegexOptions.Multiline))
                    problems.Add(entry.Prefab + " has no collider");
                if (sidecar != null && MiniJson.Str(sidecar, "rig") == "humanoid")
                {
                    string meta = File.ReadAllText(Path.Combine(root, entry.Fbx + ".meta"));
                    if (!Regex.IsMatch(meta, @"animationType: [23]\b")) problems.Add(entry.Fbx + ".meta imports no rig, so the prefab gets no Animator");
                }
            }
            return problems;
        }

        public static List<string> ScaleAndPivotSane(string root, Manifest manifest)
        {
            var problems = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                var sidecar = Sidecar(root, entry);
                if (sidecar == null) continue;
                var size = MiniJson.Object(sidecar["size"]);
                double width = MiniJson.Num(size, "width"), depth = MiniJson.Num(size, "depth"), height = MiniJson.Num(size, "height");
                double longest = Math.Max(width, Math.Max(depth, height));
                if (longest < 0.03 || longest > 40d) problems.Add(entry.Sidecar + " is " + longest.ToString("0.###", CultureInfo.InvariantCulture) + " m across");
                double floor = MiniJson.Num(sidecar, "floor");
                if (MiniJson.Str(sidecar, "pivot") == "bottom" && Math.Abs(floor) > FloorTolerance)
                    problems.Add(entry.Sidecar + " sits " + floor.ToString("0.###", CultureInfo.InvariantCulture) + " m off its bottom pivot");
                if (entry.Category == "Characters" && (height < 1.4 || height > 2.6))
                    problems.Add(entry.Sidecar + " stands " + height.ToString("0.##", CultureInfo.InvariantCulture) + " m tall in the bind pose");
            }
            return problems;
        }

        public static List<string> BoxCollidersMatchTheBindPose(string root, Manifest manifest)
        {
            var problems = new List<string>();
            foreach (var entry in manifest.Entries)
            {
                string path = Path.Combine(root, entry.Prefab);
                var sidecar = Sidecar(root, entry);
                if (!File.Exists(path) || sidecar == null) continue;
                string text = File.ReadAllText(path);
                double tilt = Tilt(text);
                if (tilt > MaxTiltDegrees)
                {
                    problems.Add(entry.Prefab + " turns its model " + tilt.ToString("0", CultureInfo.InvariantCulture) + " degrees");
                    continue;
                }
                if (tilt > 0.01) continue;
                var size = Regex.Match(text, "m_Size: " + Vector);
                var middle = Regex.Match(text, "m_Center: " + Vector);
                if (!size.Success || !middle.Success) continue;
                var box = MiniJson.Object(sidecar["size"]);
                var center = sidecar.TryGetValue("center", out var c) ? MiniJson.Object(c) : new Dictionary<string, object>();
                double w = MiniJson.Num(box, "width"), h = MiniJson.Num(box, "height"), d = MiniJson.Num(box, "depth");
                double[] wantSize = { w, h, d };
                double[] wantMiddle =
                {
                    -MiniJson.Num(center, "x") - RootValue(text, "m_LocalPosition.x"),
                    MiniJson.Num(sidecar, "floor") + h * 0.5 - RootValue(text, "m_LocalPosition.y"),
                    -MiniJson.Num(center, "y") - RootValue(text, "m_LocalPosition.z"),
                };
                for (int axis = 0; axis < 3; axis++)
                {
                    double got = Number(size.Groups[axis + 1].Value);
                    if (Math.Abs(got - wantSize[axis]) > Math.Max(SizeTolerance * wantSize[axis], 0.01))
                        problems.Add(entry.Prefab + " collider size." + "xyz"[axis] + " " + Fmt(got) + " but the bind pose is " + Fmt(wantSize[axis]) + " (run BlenderScripts/prefab_colliders.py --fix)");
                    double at = Number(middle.Groups[axis + 1].Value);
                    if (Math.Abs(at - wantMiddle[axis]) > CenterTolerance)
                        problems.Add(entry.Prefab + " collider center." + "xyz"[axis] + " " + Fmt(at) + " but the bind pose is " + Fmt(wantMiddle[axis]) + " (run BlenderScripts/prefab_colliders.py --fix)");
                }
            }
            return problems;
        }

        public static List<string> MaterialsResolved(string root, Manifest manifest)
        {
            var problems = new List<string>();
            var known = GuidIndex(root);
            foreach (var entry in manifest.Entries)
            {
                var sidecar = Sidecar(root, entry);
                if (sidecar != null && MiniJson.Strings(sidecar, "materials").Count == 0) problems.Add(entry.Sidecar + " lists no source materials");
                string baked = "Assets/Materials/Baked/" + entry.Id + ".mat";
                string bakedPath = Path.Combine(root, baked);
                if (!File.Exists(bakedPath))
                {
                    problems.Add(baked + " is missing");
                }
                else
                {
                    foreach (Match texture in Regex.Matches(File.ReadAllText(bakedPath), @"\{fileID: 2800000, guid: (\w+)"))
                    {
                        if (!known.ContainsKey(texture.Groups[1].Value)) problems.Add(baked + " points at a texture that no longer exists (" + texture.Groups[1].Value + ")");
                    }
                }
                string prefab = Path.Combine(root, entry.Prefab);
                if (!File.Exists(prefab)) continue;
                foreach (Match material in Regex.Matches(File.ReadAllText(prefab), @"\{fileID: 2100000, guid: (\w+)"))
                {
                    string guid = material.Groups[1].Value;
                    if (!known.TryGetValue(guid, out var asset)) problems.Add(entry.Prefab + " uses a material that no longer exists (" + guid + ")");
                    else if (!asset.EndsWith(".mat")) problems.Add(entry.Prefab + " uses " + asset + " as a material");
                }
            }
            return problems;
        }

        public static List<string> TriangleBudget(string root, Manifest manifest)
        {
            var problems = new List<string>();
            var budgets = Budgets(root);
            if (budgets.Count == 0) problems.Add("BlenderScripts/asset_audit.py has no BUDGETS table");
            foreach (var entry in manifest.Entries)
            {
                var sidecar = Sidecar(root, entry);
                if (sidecar == null) continue;
                int tris = (int)MiniJson.Num(sidecar, "tris");
                var lodTris = MiniJson.List(sidecar.TryGetValue("lodTris", out var lods) ? lods : null);
                int lod0 = lodTris.Count > 0 && lodTris[0] is double first ? (int)first : tris;
                if (lod0 <= 0) problems.Add(entry.Sidecar + " has no triangles");
                foreach (var budget in budgets)
                {
                    if (!("/" + entry.Fbx).Contains(budget.Key)) continue;
                    if (lod0 > budget.Value) problems.Add(entry.Fbx + " LOD0 has " + lod0 + " triangles, over the " + budget.Key.Trim('/') + " budget of " + budget.Value);
                    break;
                }
            }
            return problems;
        }

        public static List<string> ScenePathsResolve(string root, IEnumerable<string> paths)
        {
            var problems = new List<string>();
            foreach (var path in paths)
            {
                if (!File.Exists(Path.Combine(root, path))) problems.Add(path + " is referenced by code but missing");
                else if (!File.Exists(Path.Combine(root, path + ".meta"))) problems.Add(path + ".meta is missing");
            }
            return problems;
        }

        public static List<KeyValuePair<string, int>> Budgets(string root)
        {
            var budgets = new List<KeyValuePair<string, int>>();
            string audit = Path.Combine(root, "BlenderScripts", "asset_audit.py");
            if (!File.Exists(audit)) return budgets;
            var table = Regex.Match(File.ReadAllText(audit), @"BUDGETS = \((.*?)\n\)", RegexOptions.Singleline);
            foreach (Match row in Regex.Matches(table.Groups[1].Value, @"\(""([^""]+)"",\s*(\d+)\)"))
                budgets.Add(new KeyValuePair<string, int>(row.Groups[1].Value, int.Parse(row.Groups[2].Value, CultureInfo.InvariantCulture)));
            return budgets;
        }

        public static string Report(string title, List<string> problems)
        {
            if (problems.Count == 0) return "";
            var builder = new StringBuilder();
            builder.Append(title).Append(": ").Append(problems.Count).Append(" problem(s)\n");
            foreach (var problem in problems) builder.Append("  ").Append(problem).Append('\n');
            return builder.ToString();
        }

        public static Dictionary<string, string> GuidIndex(string root)
        {
            var index = new Dictionary<string, string>();
            string assets = Path.Combine(root, "Assets");
            if (!Directory.Exists(assets)) return index;
            foreach (var meta in Directory.GetFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                string guid = Guid(meta);
                if (!string.IsNullOrEmpty(guid)) index[guid] = Relative(root, meta.Substring(0, meta.Length - 5));
            }
            return index;
        }

        public static string Guid(string metaPath)
        {
            if (!File.Exists(metaPath)) return "";
            foreach (var line in File.ReadLines(metaPath))
            {
                if (line.StartsWith("guid: ")) return line.Substring(6).Trim();
            }
            return "";
        }

        public static bool PngSize(string path, out int width, out int height)
        {
            width = height = 0;
            var bytes = new byte[24];
            using (var stream = File.OpenRead(path))
            {
                if (stream.Read(bytes, 0, 24) < 24) return false;
            }
            if (bytes[0] != 0x89 || bytes[1] != 'P' || bytes[2] != 'N' || bytes[3] != 'G') return false;
            width = bytes[16] << 24 | bytes[17] << 16 | bytes[18] << 8 | bytes[19];
            height = bytes[20] << 24 | bytes[21] << 16 | bytes[22] << 8 | bytes[23];
            return true;
        }

        private static double Tilt(string text)
        {
            double w = Math.Min(1d, Math.Abs(RootValue(text, "m_LocalRotation.w", 1d)));
            return 2d * Math.Acos(w) * 180d / Math.PI;
        }

        private static double RootValue(string text, string property, double fallback = 0d)
        {
            var found = Regex.Match(text, @"target: \{fileID: " + RootTransform + @"[^\n]*\n\s+propertyPath: " + Regex.Escape(property) + @"\n\s+value: (\S+)");
            return found.Success ? Number(found.Groups[1].Value) : fallback;
        }

        private static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

        private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Relative(string root, string path) =>
            path.Substring(root.TrimEnd('/', '\\').Length + 1).Replace('\\', '/');

        private static bool Contains(byte[] haystack, string needle)
        {
            var pattern = Encoding.ASCII.GetBytes(needle);
            for (int i = 0; i <= haystack.Length - pattern.Length; i++)
            {
                int j = 0;
                while (j < pattern.Length && haystack[i + j] == pattern[j]) j++;
                if (j == pattern.Length) return true;
            }
            return false;
        }
    }
}

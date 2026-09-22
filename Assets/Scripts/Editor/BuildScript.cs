#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using OutpostZero.Shell;
using Debug = UnityEngine.Debug;

namespace OutpostZero.EditorTools
{
    public enum PlayerTarget
    {
        Windows,
        Linux,
        Mac
    }

    /// <summary>
    /// Command-line options for <see cref="BuildScript.BuildAll"/>:
    /// <c>-targets windows,linux,mac -release -version v0.1.0 -sha 1a2b3c4 -output Builds -skipScene</c>.
    /// </summary>
    public sealed class BuildArgs
    {
        public readonly List<PlayerTarget> Targets = new List<PlayerTarget>();
        public bool Release;
        public string Tag = "";
        public string Sha = "";
        public string Output = "Builds";
        public bool SkipScene;
        public readonly List<string> Problems = new List<string>();

        public static BuildArgs Parse(IList<string> args)
        {
            var parsed = new BuildArgs();
            for (int i = 0; args != null && i < args.Count; i++)
            {
                string arg = args[i];
                string next = i + 1 < args.Count ? args[i + 1] : null;
                switch (arg)
                {
                    case "-targets":
                        if (next == null) { parsed.Problems.Add("-targets needs a value"); break; }
                        i++;
                        foreach (var name in next.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (TryTarget(name.Trim(), out var target))
                            {
                                if (!parsed.Targets.Contains(target)) parsed.Targets.Add(target);
                            }
                            else parsed.Problems.Add("unknown target " + name.Trim());
                        }
                        break;
                    case "-release":
                        parsed.Release = true;
                        break;
                    case "-version":
                        if (next == null) { parsed.Problems.Add("-version needs a value"); break; }
                        parsed.Tag = next;
                        i++;
                        break;
                    case "-sha":
                        if (next == null) { parsed.Problems.Add("-sha needs a value"); break; }
                        parsed.Sha = next;
                        i++;
                        break;
                    case "-output":
                        if (next == null) { parsed.Problems.Add("-output needs a value"); break; }
                        parsed.Output = next;
                        i++;
                        break;
                    case "-skipScene":
                        parsed.SkipScene = true;
                        break;
                }
            }
            if (parsed.Targets.Count == 0)
            {
                parsed.Targets.Add(PlayerTarget.Windows);
                parsed.Targets.Add(PlayerTarget.Linux);
                parsed.Targets.Add(PlayerTarget.Mac);
            }
            if (!string.IsNullOrEmpty(parsed.Tag) && !ReleaseVersion.TryParse(parsed.Tag, out _))
                parsed.Problems.Add("-version " + parsed.Tag + " is not a semantic version");
            return parsed;
        }

        public static bool TryTarget(string name, out PlayerTarget target)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "win":
                case "win64":
                case "windows":
                    target = PlayerTarget.Windows;
                    return true;
                case "linux":
                case "linux64":
                    target = PlayerTarget.Linux;
                    return true;
                case "mac":
                case "macos":
                case "osx":
                    target = PlayerTarget.Mac;
                    return true;
            }
            target = PlayerTarget.Windows;
            return false;
        }

        /// <summary>Folder and executable for one target under the output root, e.g. Builds/Linux/OutpostZero.x86_64.</summary>
        public static string PlayerPath(string output, PlayerTarget target)
        {
            string root = string.IsNullOrEmpty(output) ? "Builds" : output;
            switch (target)
            {
                case PlayerTarget.Windows: return Path.Combine(root, "Windows", "OutpostZero.exe");
                case PlayerTarget.Linux: return Path.Combine(root, "Linux", "OutpostZero.x86_64");
                default: return Path.Combine(root, "macOS", "OutpostZero.app");
            }
        }

        public static BuildTarget UnityTarget(PlayerTarget target)
        {
            switch (target)
            {
                case PlayerTarget.Windows: return BuildTarget.StandaloneWindows64;
                case PlayerTarget.Linux: return BuildTarget.StandaloneLinux64;
                default: return BuildTarget.StandaloneOSX;
            }
        }

        /// <summary>Release players are IL2CPP with engine stripping and LZ4HC; dev players are Mono, LZ4, and development builds.</summary>
        public static BuildOptions Options(bool release)
        {
            return release ? BuildOptions.CompressWithLz4HC : BuildOptions.Development | BuildOptions.CompressWithLz4;
        }

        public static ScriptingImplementation Backend(bool release)
        {
            return release ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x;
        }
    }

    public static class BuildScript
    {
        public const string StreamingVersion = "Assets/StreamingAssets/" + BuildStamp.FileName;
        public const string ResourcesVersion = "Assets/Resources/version.json";
        public const string Unity = "6000.0.83f1";

        public static readonly string[] Scenes = { "Assets/Scenes/Boot.unity", "Assets/Scenes/PrototypeArena.unity" };

        [MenuItem("Tools/Outpost Zero/Build Players/All (dev)", false, 20)]
        public static void BuildAllDevMenu() => Run(BuildArgs.Parse(new[] { "-skipScene" }), false);

        [MenuItem("Tools/Outpost Zero/Build Players/Windows (dev)", false, 21)]
        public static void BuildWindowsDevMenu() => Run(One(PlayerTarget.Windows), false);

        [MenuItem("Tools/Outpost Zero/Build Players/Linux (dev)", false, 22)]
        public static void BuildLinuxDevMenu() => Run(One(PlayerTarget.Linux), false);

        [MenuItem("Tools/Outpost Zero/Build Players/macOS (dev)", false, 23)]
        public static void BuildMacDevMenu() => Run(One(PlayerTarget.Mac), false);

        /// <summary>Batch entry point: <c>-batchmode -nographics -executeMethod OutpostZero.EditorTools.BuildScript.BuildAll</c>.</summary>
        public static void BuildAll()
        {
            var args = BuildArgs.Parse(Environment.GetCommandLineArgs());
            Run(args, Application.isBatchMode);
        }

        private static BuildArgs One(PlayerTarget target)
        {
            var args = new BuildArgs { SkipScene = true };
            args.Targets.Add(target);
            return args;
        }

        public static bool Run(BuildArgs args, bool exitWhenDone)
        {
            bool ok = true;
            try
            {
                if (args.Problems.Count > 0) throw new ArgumentException(string.Join("; ", args.Problems));
                if (!args.SkipScene || !File.Exists(Scenes[1])) PrototypeSceneBuilder.BuildAndSaveSceneBatch();
                var version = Resolve(args);
                string sha = args.Sha.Length > 0 ? args.Sha : Environment.GetEnvironmentVariable("GITHUB_SHA") ?? Git("rev-parse HEAD");
                var report = new StringBuilder();
                report.Append("{\n  \"version\": \"").Append(version).Append("\",\n  \"release\": ").Append(args.Release ? "true" : "false").Append(",\n  \"players\": [");
                for (int i = 0; i < args.Targets.Count; i++)
                {
                    var result = Build(args.Targets[i], version, sha, args);
                    ok &= result.summary.result == BuildResult.Succeeded;
                    if (i > 0) report.Append(',');
                    report.Append("\n    { \"target\": \"").Append(args.Targets[i]).Append("\", \"result\": \"").Append(result.summary.result)
                        .Append("\", \"bytes\": ").Append(result.summary.totalSize).Append(", \"seconds\": ").Append(((int)result.summary.totalTime.TotalSeconds).ToString())
                        .Append(", \"errors\": ").Append(result.summary.totalErrors).Append(" }");
                }
                report.Append("\n  ]\n}\n");
                Directory.CreateDirectory(args.Output);
                File.WriteAllText(Path.Combine(args.Output, "build-report.json"), report.ToString());
                Debug.Log("[BuildScript] " + version + " " + (ok ? "built" : "FAILED") + "\n" + report);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ok = false;
            }
            if (exitWhenDone) EditorApplication.Exit(ok ? 0 : 1);
            return ok;
        }

        public static ReleaseVersion Resolve(BuildArgs args)
        {
            string tag = args.Tag;
            if (string.IsNullOrEmpty(tag))
            {
                string refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
                if (ReleaseVersion.TryParse(refName, out _)) tag = refName;
            }
            if (string.IsNullOrEmpty(tag)) tag = Git("describe --tags --abbrev=0 --match v*");
            string fallback = File.Exists(ResourcesVersion) ? BuildStamp.Parse(File.ReadAllText(ResourcesVersion)) : SceneRoute.Version;
            string sha = args.Sha.Length > 0 ? args.Sha : Environment.GetEnvironmentVariable("GITHUB_SHA") ?? Git("rev-parse HEAD");
            return ReleaseVersion.Stamp(tag, fallback, sha, args.Release);
        }

        private static BuildReport Build(PlayerTarget target, ReleaseVersion version, string sha, BuildArgs args)
        {
            var named = NamedBuildTarget.Standalone;
            var backend = PlayerSettings.GetScriptingBackend(named);
            bool strip = PlayerSettings.stripEngineCode;
            string bundle = PlayerSettings.bundleVersion;
            bool hadVersion = File.Exists(StreamingVersion);
            try
            {
                PlayerSettings.SetScriptingBackend(named, BuildArgs.Backend(args.Release));
                PlayerSettings.stripEngineCode = args.Release;
                PlayerSettings.bundleVersion = version.Core;
                if (target == PlayerTarget.Mac) EditorUserBuildSettings.SetPlatformSettings("Standalone", "OSXUniversal", "Architecture", "x64ARM64");
                Directory.CreateDirectory(Path.GetDirectoryName(StreamingVersion));
                File.WriteAllText(StreamingVersion, version.Json(sha, Unity, args.Release ? "release" : "dev"));
                AssetDatabase.ImportAsset(StreamingVersion);

                string location = BuildArgs.PlayerPath(args.Output, target);
                Directory.CreateDirectory(Path.GetDirectoryName(location));
                Debug.Log("[BuildScript] " + target + " " + version + " (" + BuildArgs.Backend(args.Release) + ") -> " + location);
                return BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = Scenes,
                    locationPathName = location,
                    target = BuildArgs.UnityTarget(target),
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildArgs.Options(args.Release),
                });
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(named, backend);
                PlayerSettings.stripEngineCode = strip;
                PlayerSettings.bundleVersion = bundle;
                if (!hadVersion) AssetDatabase.DeleteAsset(StreamingVersion);
            }
        }

        private static string Git(string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var process = Process.Start(info))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0 ? output : "";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
#endif

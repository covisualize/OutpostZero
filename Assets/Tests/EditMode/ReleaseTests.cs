using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OutpostZero.EditorTools;
using OutpostZero.Graphics;
using OutpostZero.Shell;
using OutpostZero.Utils;

namespace OutpostZero.Tests.EditMode
{
    public class ReleaseTests
    {
        [Test]
        public void TagsParseAsSemanticVersions()
        {
            Assert.IsTrue(ReleaseVersion.TryParse("v1.2.3", out var plain));
            Assert.AreEqual("1.2.3", plain.ToString());
            Assert.IsFalse(plain.IsPrerelease);
            Assert.IsTrue(ReleaseVersion.TryParse("v1.0.0-rc.1", out var rc));
            Assert.AreEqual("rc.1", rc.Pre);
            Assert.IsTrue(rc.IsPrerelease);
            Assert.IsTrue(ReleaseVersion.TryParse("0.5.0+abc", out var meta));
            Assert.AreEqual("abc", meta.Meta);
            Assert.IsFalse(ReleaseVersion.TryParse("1.0", out _));
            Assert.IsFalse(ReleaseVersion.TryParse("release-7", out _));
            Assert.IsFalse(ReleaseVersion.TryParse(null, out _));
        }

        [Test]
        public void DevBuildsCarryTheShortCommitAndReleasesTheTag()
        {
            string sha = "1a2b3c4d5e6f7a8b9c0d";
            Assert.AreEqual("1.0.0-rc.1", ReleaseVersion.Stamp("v1.0.0-rc.1", "0.5.0", sha, true).ToString());
            Assert.AreEqual("0.3.0-dev+1a2b3c4", ReleaseVersion.Stamp("v0.3.0", "0.5.0", sha, false).ToString());
            Assert.AreEqual("0.5.0-dev+1a2b3c4", ReleaseVersion.Stamp("", "0.5.0", sha, false).ToString());
            Assert.AreEqual("1.0.0-rc.1+1a2b3c4", ReleaseVersion.Stamp("v1.0.0-rc.1", "", sha, false).ToString());
            Assert.AreEqual("0.0.0-dev", ReleaseVersion.Stamp("junk", "junk", "", false).ToString());
            Assert.AreEqual("1.0.0", ReleaseVersion.Stamp("v1.0.0+old", "", sha, true).ToString());
            Assert.AreEqual("0.3.0", ReleaseVersion.Stamp("v0.3.0", "", sha, false).Core);
        }

        [Test]
        public void PrereleasesSortBelowTheirRelease()
        {
            ReleaseVersion Parse(string text)
            {
                Assert.IsTrue(ReleaseVersion.TryParse(text, out var v), text);
                return v;
            }
            string[] ordered = { "0.9.9", "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0", "1.0.1", "1.1.0", "2.0.0" };
            for (int i = 1; i < ordered.Length; i++)
                Assert.Less(Parse(ordered[i - 1]).CompareTo(Parse(ordered[i])), 0, ordered[i - 1] + " < " + ordered[i]);
            Assert.AreEqual(0, Parse("1.0.0+a").CompareTo(Parse("1.0.0+b")));
        }

        [Test]
        public void VersionJsonRoundTripsThroughBuildStamp()
        {
            Assert.IsTrue(ReleaseVersion.TryParse("v0.6.0-rc.2", out var version));
            string json = version.Json("deadbeefcafe", "6000.0.83f1", "release");
            Assert.AreEqual("0.6.0-rc.2", BuildStamp.Parse(json));
            Assert.AreEqual("deadbee", BuildStamp.ParseCommit(json));
            StringAssert.Contains("\"channel\": \"release\"", json);
            Assert.AreEqual("", BuildStamp.ParseCommit(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "version.json"))));
        }

        [Test]
        public void BuildArgsReadTargetsReleaseAndVersion()
        {
            var args = BuildArgs.Parse(new[] { "Unity", "-batchmode", "-targets", "linux,win64,linux", "-release", "-version", "v0.1.0", "-sha", "abc", "-output", "Out" });
            CollectionAssert.AreEqual(new[] { PlayerTarget.Linux, PlayerTarget.Windows }, args.Targets);
            Assert.IsTrue(args.Release);
            Assert.AreEqual("v0.1.0", args.Tag);
            Assert.AreEqual("abc", args.Sha);
            Assert.AreEqual("Out", args.Output);
            CollectionAssert.IsEmpty(args.Problems);

            var all = BuildArgs.Parse(new[] { "-skipScene" });
            CollectionAssert.AreEqual(new[] { PlayerTarget.Windows, PlayerTarget.Linux, PlayerTarget.Mac }, all.Targets);
            Assert.IsTrue(all.SkipScene);
            Assert.IsFalse(all.Release);

            var bad = BuildArgs.Parse(new[] { "-targets", "ps5", "-version", "latest", "-output" });
            Assert.AreEqual(3, bad.Problems.Count);
        }

        [Test]
        public void ReleaseAndDevPlayersDifferInBackendAndCompression()
        {
            Assert.AreEqual(ScriptingImplementation.IL2CPP, BuildArgs.Backend(true));
            Assert.AreEqual(ScriptingImplementation.Mono2x, BuildArgs.Backend(false));
            Assert.AreEqual(BuildOptions.CompressWithLz4HC, BuildArgs.Options(true));
            Assert.IsTrue((BuildArgs.Options(false) & BuildOptions.Development) != 0);
            Assert.AreEqual(BuildTarget.StandaloneOSX, BuildArgs.UnityTarget(PlayerTarget.Mac));
            Assert.AreEqual(BuildTarget.StandaloneLinux64, BuildArgs.UnityTarget(PlayerTarget.Linux));
            Assert.AreEqual(Path.Combine("Builds", "Windows", "OutpostZero.exe"), BuildArgs.PlayerPath("", PlayerTarget.Windows));
            Assert.AreEqual(Path.Combine("B", "macOS", "OutpostZero.app"), BuildArgs.PlayerPath("B", PlayerTarget.Mac));
            CollectionAssert.AreEqual(new[] { "Assets/Scenes/Boot.unity", "Assets/Scenes/PrototypeArena.unity" }, BuildScript.Scenes);
            foreach (var scene in BuildScript.Scenes) Assert.IsTrue(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scene)), scene);
        }

        [Test]
        public void LogsRotateOldestFirstAndKeepFive()
        {
            Assert.AreEqual("outpost.log", LogRotation.Name(0));
            Assert.AreEqual("outpost.3.log", LogRotation.Name(3));
            var moves = LogRotation.Moves(new[] { "outpost.log", "outpost.1.log", "outpost.3.log" }, 5);
            Assert.AreEqual(3, moves.Count);
            Assert.AreEqual("outpost.3.log", moves[0].Key);
            Assert.AreEqual("outpost.4.log", moves[0].Value);
            Assert.AreEqual("outpost.1.log", moves[1].Key);
            Assert.AreEqual("outpost.log", moves[2].Key);
            Assert.AreEqual("outpost.1.log", moves[2].Value);
            CollectionAssert.IsEmpty(LogRotation.Moves(new string[0], 5));
            Assert.IsTrue(LogRotation.ShouldRoll(LogRotation.Cap, LogRotation.Cap));
            Assert.IsFalse(LogRotation.ShouldRoll(10, 0));
        }

        [Test]
        public void LogLinesKeepStacksOnlyForFaults()
        {
            var at = new DateTime(2026, 9, 22, 10, 15, 0, DateTimeKind.Utc);
            Assert.AreEqual("2026-09-22T10:15:00.000Z [Log] hello\n", LogRotation.Line(at, LogType.Log, "hello  ", "frame A\n"));
            string fault = LogRotation.Line(at, LogType.Exception, "NullReferenceException", "A.B ()\r\nC.D ()\n");
            Assert.AreEqual("2026-09-22T10:15:00.000Z [Exception] NullReferenceException\n    A.B ()\n    C.D ()\n", fault);

            string clean = LogRotation.Line(at, LogType.Log, "session opened", null) + LogRotation.Line(at, LogType.Log, LogRotation.Closing, null);
            Assert.IsFalse(LogRotation.Crashed(clean));
            Assert.IsTrue(LogRotation.Crashed(LogRotation.Line(at, LogType.Log, "session opened", null)));
            Assert.IsTrue(LogRotation.Crashed(fault + LogRotation.Line(at, LogType.Log, LogRotation.Closing, null)));
            Assert.IsFalse(LogRotation.Crashed(""));
        }

        [Test]
        public void ScreenTourCoversThirtySecondsOfDistinctMarks()
        {
            Assert.AreEqual(30f, ScreenTour.Length, 0.001f);
            Assert.AreEqual(0, ScreenTour.At(0f));
            Assert.AreEqual(1, ScreenTour.At(5f));
            Assert.AreEqual(5, ScreenTour.At(29.9f));
            Assert.AreEqual(-1, ScreenTour.At(30f));
            Assert.Less(ScreenTour.Settle, ScreenTour.Hold);
            var weathers = new System.Collections.Generic.HashSet<WeatherKind>();
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var mark in ScreenTour.Marks)
            {
                Assert.IsTrue(names.Add(mark.Name), mark.Name);
                weathers.Add(mark.Weather);
                Assert.Greater(mark.Position.y, mark.Target.y, mark.Name);
                Assert.That(mark.Night, Is.InRange(0f, 1f));
            }
            Assert.AreEqual(5, weathers.Count);

            Assert.AreEqual("out", ScreenTour.Folder(new[] { "-screenshotDir", "out" }, "env", "p"));
            Assert.AreEqual("env", ScreenTour.Folder(new[] { "-screenshotDir" }, "env", "p"));
            Assert.AreEqual(Path.Combine("p", "Screenshots"), ScreenTour.Folder(null, null, "p"));
            Assert.IsTrue(ScreenTour.Requested(new[] { "-screenshotTour" }, null));
            Assert.IsTrue(ScreenTour.Requested(null, "1"));
            Assert.IsFalse(ScreenTour.Requested(new[] { "-batchmode" }, "0"));
            StringAssert.Contains("\"a.png\",\n    \"b.png\"", ScreenTour.Index(new[] { "a.png", "b.png" }, "1.0.0"));
        }
    }
}

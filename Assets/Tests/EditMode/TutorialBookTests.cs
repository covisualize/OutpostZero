using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-67: the Day 1 and first-expedition steps are TutorialStepDefinition assets listed in Resources/TutorialBook.</summary>
    public class TutorialBookTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Root, relative)).Replace("\r\n", "\n");
        }

        private static string Guid(string metaRelative)
        {
            return Regex.Match(Read(metaRelative), "guid: (\\w+)").Groups[1].Value;
        }

        private static string Field(string asset, string name)
        {
            string value = Regex.Match(asset, "^  " + name + ": ?(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'') value = value.Substring(1, value.Length - 2).Replace("''", "'");
            return value;
        }

        private static List<string> Listed(string book, string track)
        {
            var block = Regex.Match(book, "^  " + track + ":\\n((?:  - .*\\n)*)", RegexOptions.Multiline);
            Assert.IsTrue(block.Success, track + " list");
            var guids = new List<string>();
            foreach (Match m in Regex.Matches(block.Groups[1].Value, "guid: (\\w+), type: 2")) guids.Add(m.Groups[1].Value);
            return guids;
        }

        [TearDown]
        public void Reset()
        {
            TutorialTrack.Clear();
        }

        [TestCase("camp")]
        [TestCase("street")]
        public void EachTrackIsListedInOrderAndMatchesTheBuiltInSteps(string track)
        {
            string book = Read("Assets/Resources/" + TutorialBook.ResourcePath + ".asset");
            StringAssert.Contains(Guid("Assets/Scripts/Shell/TutorialBook.cs.meta"), book);
            var steps = track == TutorialTrack.CampTrack ? TutorialTrack.CodeCamp : TutorialTrack.CodeStreet;
            var listed = Listed(book, track);
            Assert.AreEqual(steps.Length, listed.Count, track);
            string script = Guid("Assets/Scripts/Shell/TutorialStepDefinition.cs.meta");
            for (int i = 0; i < steps.Length; i++)
            {
                string path = "Assets/Data/Tutorial/" + steps[i].Key.Replace(".", "_") + ".asset";
                Assert.IsTrue(File.Exists(Path.Combine(Root, path)), path);
                Assert.AreEqual(listed[i], Guid(path + ".meta"), path + " is listed out of order");
                string asset = Read(path);
                StringAssert.Contains(script, asset, path);
                Assert.AreEqual(track, Field(asset, "track"), path);
                Assert.AreEqual(steps[i].Key, Field(asset, "key"), path);
                Assert.AreEqual(steps[i].Fallback, Field(asset, "fallback"), path);
                Assert.AreEqual(steps[i].Gate, Field(asset, "gate"), path);
                Assert.AreEqual(steps[i].Mark, Field(asset, "mark"), path);
            }
        }

        [Test]
        public void ABookOverridesTheTracksAndAnEmptyTrackFallsBack()
        {
            var custom = new[] { new TutorialStep("day1.x", "Light the lamp.", "lamp", TutorialMark.Stores) };
            TutorialTrack.Use(custom, new List<TutorialStep>());
            Assert.IsTrue(TutorialTrack.FromAsset);
            Assert.AreEqual(1, TutorialTrack.Camp.Length);
            Assert.AreEqual("lamp", TutorialTrack.Camp[0].Gate);
            Assert.AreSame(TutorialTrack.CodeStreet, TutorialTrack.Street);
            int index = TutorialTrack.Advance(TutorialTrack.Camp, 0, "lamp", out bool finished);
            Assert.AreEqual(1, index);
            Assert.IsTrue(finished);

            TutorialTrack.Use(new[] { new TutorialStep("", "No key.", "x"), new TutorialStep("day1.y", "No gate.", "") }, null);
            Assert.IsFalse(TutorialTrack.FromAsset, "steps without a key or gate are dropped");
            Assert.AreSame(TutorialTrack.CodeCamp, TutorialTrack.Camp);
        }

        [Test]
        public void EveryStreetStepReadsFromTheStringTable()
        {
            foreach (var step in TutorialTrack.CodeStreet)
                Assert.AreEqual(step.Fallback, Loc.Raw(step.Key, "en"), step.Key);
            foreach (var step in TutorialTrack.CodeCamp)
                Assert.AreNotEqual(step.Key, Loc.Raw(step.Key, "es"), step.Key + " has no Spanish line");
            foreach (var step in TutorialTrack.CodeStreet)
                Assert.AreNotEqual(step.Key, Loc.Raw(step.Key, "es"), step.Key + " has no Spanish line");
        }
    }
}

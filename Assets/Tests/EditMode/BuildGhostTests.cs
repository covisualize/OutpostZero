using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-58: the ghost verdict before a click, and finished modules wearing the baked base-kit prefabs.
    /// </summary>
    public class BuildGhostTests
    {
        static string Root => Directory.GetCurrentDirectory();

        [Test]
        public void SnapLandsOnTheTwoMetreGrid()
        {
            Assert.AreEqual(4f, BuildGhost.Snap(3.2f, 2f));
            Assert.AreEqual(-2f, BuildGhost.Snap(-2.9f, 2f));
            Assert.AreEqual(1.3f, BuildGhost.Snap(1.3f, 0f));
        }

        [Test]
        public void VerdictsCoverTakenOutsideAndShort()
        {
            var placed = new List<PlacedModule> { new PlacedModule { kind = "Cot", x = 4f, z = 2f } };
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(placed, 6f, 2f, 10, 10));
            Assert.AreEqual(BuildGhost.Verdict.Taken, BuildGhost.Check(placed, 4f, 2f, 10, 99));
            Assert.AreEqual(BuildGhost.Verdict.Outside, BuildGhost.Check(placed, MapRim.Half + 2f, 0f, 0, 99));
            Assert.AreEqual(BuildGhost.Verdict.Short, BuildGhost.Check(placed, 6f, 2f, 11, 10));
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(null, 0f, 0f, 0, 0));
            Assert.AreNotEqual(BuildGhost.Tint(BuildGhost.Verdict.Ok), BuildGhost.Tint(BuildGhost.Verdict.Short));
        }

        [Test]
        public void EveryLookIsAModuleKindOnABakedPrefabRoot()
        {
            string asset = File.ReadAllText(Path.Combine(Root, "Assets/Resources/ModuleLooks.asset"));
            string script = Regex.Match(File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Colony/ModuleLooks.cs.meta")), @"guid: (\w+)").Groups[1].Value;
            StringAssert.Contains("guid: " + script, asset);

            var prefabs = new Dictionary<string, string>();
            foreach (var meta in Directory.GetFiles(Path.Combine(Root, "Assets/Prefabs"), "*.prefab.meta", SearchOption.AllDirectories))
                prefabs[Regex.Match(File.ReadAllText(meta), @"guid: (\w+)").Groups[1].Value] = meta.Substring(0, meta.Length - 5);

            var looks = Regex.Matches(asset, @"- kind: (\w+)\s+prefab: \{fileID: (\d+), guid: (\w+), type: 3\}");
            Assert.GreaterOrEqual(looks.Count, 12);
            var kinds = new HashSet<string>();
            foreach (Match look in looks)
            {
                string kind = look.Groups[1].Value;
                Assert.IsTrue(Enum.IsDefined(typeof(ModuleKind), kind), kind);
                Assert.IsTrue(kinds.Add(kind), "duplicate " + kind);
                Assert.IsTrue(prefabs.TryGetValue(look.Groups[3].Value, out var prefab), kind + " prefab guid");
                StringAssert.Contains("--- !u!1 &" + look.Groups[2].Value + " stripped", File.ReadAllText(prefab), kind + " root");
            }
            foreach (var needed in new[] { "Barricade", "Generator", "Water", "Purifier", "Cot", "Watchtower", "Workbench", "Campfire" })
                CollectionAssert.Contains(kinds, needed);
        }

        [Test]
        public void EveryModuleSitsOnOneTabWithALabel()
        {
            var seen = new HashSet<ModuleKind>();
            for (int t = 0; t < BuildMenu.TabCount; t++)
            {
                foreach (var kind in BuildMenu.Kinds((BuildMenu.Tab)t))
                {
                    Assert.IsTrue(seen.Add(kind), kind + " on two tabs");
                    Assert.AreEqual((BuildMenu.Tab)t, BuildMenu.TabOf(kind));
                    Assert.AreNotEqual(BuildMenu.LabelKey(kind), OutpostZero.Shell.Loc.T(BuildMenu.LabelKey(kind), "en"), kind + " label");
                }
                string tabKey = BuildMenu.TabKey((BuildMenu.Tab)t);
                Assert.AreNotEqual(tabKey, OutpostZero.Shell.Loc.T(tabKey, "es"));
            }
            Assert.AreEqual(Enum.GetValues(typeof(ModuleKind)).Length, seen.Count);
            Assert.IsTrue(BuildMenu.Affordable(10, 10));
            Assert.IsFalse(BuildMenu.Affordable(11, 10));
        }

        [Test]
        public void FindReadsTheTable()
        {
            var looks = new[] { new ModuleLooks.Look { kind = "Cot", prefab = null } };
            Assert.IsNull(ModuleLooks.Find(looks, "Generator"));
            Assert.IsNull(ModuleLooks.Find(null, "Cot"));
            Assert.IsNull(ModuleLooks.Find(looks, ""));
        }
    }
}

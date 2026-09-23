using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-24: weapon mods as data, with the suppressor at the issue's 0.35 noise and a quiet report.</summary>
    public class WeaponModBookTests
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

        private static Dictionary<string, string> Fields(string asset)
        {
            var fields = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(asset, "^  (\\w+): ?(.*)$", RegexOptions.Multiline)) fields[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            return fields;
        }

        private static float F(Dictionary<string, string> f, string key) => float.Parse(f[key], CultureInfo.InvariantCulture);

        [TearDown]
        public void ClearBook()
        {
            WeaponModTable.Clear();
        }

        [Test]
        public void FourModAssetsMatchTheBuiltInRowsInBookOrder()
        {
            string book = Read("Assets/Resources/WeaponModBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Combat/WeaponModBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);
            var rows = WeaponModTable.BuiltInRows();
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual(rows.Count, listed.Count);
            string script = Guid("Assets/Scripts/Combat/WeaponModDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string path = "Assets/Data/WeaponMods/" + row.Id + ".asset";
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Damage, F(f, "damage"), 0.0001f, row.Id);
                Assert.AreEqual(row.Noise, F(f, "noise"), 0.0001f, row.Id);
                Assert.AreEqual(row.Spread, F(f, "spread"), 0.0001f, row.Id);
                Assert.AreEqual(row.MagazineBonus.ToString(), f["magazineBonus"], row.Id);
                Assert.AreEqual(row.Quiet ? "1" : "0", f["quiet"], row.Id);
                Assert.IsTrue(CraftBill.TryOf(row.Id, out _), row.Id + " has no recipe");
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "WeaponMods"), "*.asset").Length, "no stray mod assets");
            StringAssert.Contains("WeaponModBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }

        [Test]
        public void TheSuppressorCutsNoiseToTheIssuesShareAndQuietsTheReport()
        {
            var suppressor = WeaponMod.ProfileFor("suppressor");
            Assert.AreEqual(0.35f, suppressor.noise, 0.0001f);
            Assert.IsTrue(suppressor.quiet);
            Assert.IsFalse(WeaponMod.ProfileFor("optic").quiet);
            Assert.AreEqual(NoiseType.GunshotQuiet, WeaponMod.Report(NoiseType.GunshotLoud, suppressor.quiet), "the loud-shot spawn call never hears it");
            Assert.AreEqual(NoiseType.GunshotLoud, WeaponMod.Report(NoiseType.GunshotLoud, false));
            StringAssert.Contains("mod != null && mod.Quiet", Read("Assets/Scripts/Combat/WeaponBase.cs"));
            StringAssert.Contains("type == NoiseType.GunshotLoud || type == NoiseType.Explosion", Read("Assets/Scripts/AI/ZombieSpawner.cs"));
        }

        [Test]
        public void StackedModsMultiplyAndAddAndAnAssetOverridesTheCode()
        {
            var all = WeaponMod.Combine("suppressor+optic+extended_mag+rail");
            Assert.AreEqual(0.9f, all.damage, 0.0001f);
            Assert.AreEqual(0.35f, all.noise, 0.0001f);
            Assert.AreEqual(0.85f * 0.55f, all.spread, 0.0001f);
            Assert.AreEqual(10, all.magazineBonus);
            Assert.IsTrue(all.quiet);
            Assert.AreEqual(1f, WeaponMod.Combine("bogus").noise, 0.0001f, "an unknown id changes nothing");

            WeaponModTable.Use(new List<WeaponModTable.Row>
            {
                new WeaponModTable.Row { Id = "extended_mag", MagazineBonus = 15 },
                new WeaponModTable.Row { Id = "suppressor", Noise = 0.5f, Quiet = false }
            });
            Assert.AreEqual(15, WeaponMod.ProfileFor("extended_mag").magazineBonus);
            Assert.AreEqual(0.5f, WeaponMod.ProfileFor("suppressor").noise, 0.0001f);
            Assert.IsFalse(WeaponMod.ProfileFor("suppressor").quiet);
            Assert.AreEqual(0.55f, WeaponMod.ProfileFor("optic").spread, 0.0001f, "ids the book lacks fall back to the code");
        }
    }
}

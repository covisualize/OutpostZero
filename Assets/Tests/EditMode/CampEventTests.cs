using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-56: eight weighted random camp events, authored as CampEventDefinition assets and rolled at dawn.</summary>
    public class CampEventTests
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

        private static CampEventContext Busy(int day)
        {
            return new CampEventContext { Day = day, Room = true, Rain = true, Feud = true, Healthy = true, MerchantAway = true, Generator = true, Water = true };
        }

        [TearDown]
        public void Reset()
        {
            CampEventTable.Clear();
        }

        [Test]
        public void EachEventWaitsOnItsOwnConditions()
        {
            var quiet = new CampEventContext { Day = 10 };
            var expect = new Dictionary<string, System.Func<CampEventContext, CampEventContext>>
            {
                { CampEventTable.Stranger, c => { c.Room = true; return c; } },
                { CampEventTable.Argument, c => { c.Feud = true; return c; } },
                { CampEventTable.Generator, c => { c.Generator = true; return c; } },
                { CampEventTable.Sickness, c => { c.Healthy = true; return c; } },
                { CampEventTable.Merchant, c => { c.MerchantAway = true; return c; } },
            };
            foreach (var pair in expect)
            {
                var row = CampEventTable.Find(pair.Key);
                Assert.IsFalse(CampEventTable.Eligible(row, quiet), pair.Key + " without its condition");
                Assert.IsTrue(CampEventTable.Eligible(row, pair.Value(quiet)), pair.Key + " with its condition");
            }
            var rain = CampEventTable.Find(CampEventTable.RainFill);
            Assert.IsFalse(CampEventTable.Eligible(rain, new CampEventContext { Day = 10, Rain = true }), "rain needs a collector");
            Assert.IsFalse(CampEventTable.Eligible(rain, new CampEventContext { Day = 10, Water = true }), "a collector needs rain");
            Assert.IsTrue(CampEventTable.Eligible(rain, new CampEventContext { Day = 10, Rain = true, Water = true }));
            Assert.IsTrue(CampEventTable.Eligible(CampEventTable.Find(CampEventTable.Cache), quiet));
            Assert.IsFalse(CampEventTable.Eligible(CampEventTable.Find(CampEventTable.Sighting), new CampEventContext { Day = 3 }));
            Assert.IsTrue(CampEventTable.Eligible(CampEventTable.Find(CampEventTable.Sighting), new CampEventContext { Day = 4 }));
        }

        [Test]
        public void DawnsRollTheSameEventForTheSameSeedAndAllEightTurnUp()
        {
            var seen = new HashSet<string>();
            int quiet = 0;
            for (int day = 2; day < 402; day++)
            {
                string id = CampEventTable.Roll(Busy(day), 1701);
                Assert.AreEqual(id, CampEventTable.Roll(Busy(day), 1701), "day " + day);
                if (id.Length == 0) quiet++;
                else seen.Add(id);
            }
            Assert.AreEqual(8, CampEventTable.Code.Length);
            Assert.AreEqual(8, seen.Count, string.Join(",", seen));
            Assert.That(quiet, Is.InRange(130, 230), "about 45% of dawns are quiet");
            Assert.AreEqual("", CampEventTable.Roll(Busy(1), 1701), "Day 1 is the tutorial");
        }

        [Test]
        public void AQuietCampOnlyFindsTheCacheOrSeesAPack()
        {
            for (int day = 2; day < 60; day++)
            {
                string id = CampEventTable.Roll(new CampEventContext { Day = day }, 99);
                bool pack = day >= 4 && id == CampEventTable.Sighting;
                Assert.That(id == "" || id == CampEventTable.Cache || pack, day + ": " + id);
            }
        }

        [Test]
        public void RepairsNeedAnEngineerAndParts()
        {
            Assert.IsTrue(CampEventTable.CanRepair(3, 3, 1));
            Assert.IsFalse(CampEventTable.CanRepair(2, 10, 5));
            Assert.IsFalse(CampEventTable.CanRepair(5, 2, 5));
            Assert.IsFalse(CampEventTable.CanRepair(5, 10, 0));
            StringAssert.Contains("Engineering 3", CampEventDirector.RepairNeed("en"));
        }

        [Test]
        public void TheLogSurvivesASave()
        {
            var log = CampEventLog.Fresh;
            log.RolledDay = 7;
            log.LastId = CampEventTable.Stranger;
            log.LastDay = 7;
            log.LastLine = "A stranger (Kai | Moss)";
            log.StrangerId = "stranger_7_42";
            log.StrangerName = "Kai Moss";
            log.StrangerTrait = "Cook";
            log.StrangerDay = 7;
            log.SightedDay = 6;
            log.GeneratorBroken = true;
            var back = CampEventLog.Unpack(log.Pack());
            Assert.AreEqual(7, back.RolledDay);
            Assert.AreEqual("A stranger (Kai / Moss)", back.LastLine);
            Assert.AreEqual("Kai Moss", back.StrangerName);
            Assert.AreEqual(7, back.StrangerDay);
            Assert.AreEqual(-1, back.MerchantDay);
            Assert.AreEqual(6, back.SightedDay);
            Assert.IsTrue(back.GeneratorBroken);
            Assert.AreEqual(-1, CampEventLog.Unpack("").RolledDay);
            Assert.AreEqual(-1, CampEventLog.Unpack("9|x").RolledDay);
        }

        [Test]
        public void TheStrangerIsSeededByDay()
        {
            var a = SurvivorDraw.Stranger(1701, 5);
            Assert.AreEqual(a.Id, SurvivorDraw.Stranger(1701, 5).Id);
            Assert.AreEqual(a.Name, SurvivorDraw.Stranger(1701, 5).Name);
            StringAssert.StartsWith("stranger_5_", a.Id);
            Assert.AreNotEqual(a.Id, SurvivorDraw.Stranger(1701, 6).Id);
            Assert.IsNotEmpty(a.Trait);
        }

        [Test]
        public void ABookOverridesTheRowsAndAnEmptyBookFallsBack()
        {
            CampEventTable.Use(new[] { CampEventTable.Row("cache", 1, 5, "", false, false, false, false, false, "event.cache", "x") });
            Assert.IsTrue(CampEventTable.FromAsset);
            Assert.AreEqual(1, CampEventTable.Rows.Length);
            Assert.AreEqual(5, CampEventTable.Find("cache").MinDay);
            CampEventTable.Use(new[] { CampEventTable.Row("cache", 0, 5, "", false, false, false, false, false, "", "") });
            Assert.IsFalse(CampEventTable.FromAsset, "zero-weight rows are dropped");
            Assert.AreEqual(8, CampEventTable.Rows.Length);
        }

        [Test]
        public void TheBookListsEachEventAsAuthored()
        {
            string book = Read("Assets/Resources/" + CampEventBook.ResourcePath + ".asset");
            StringAssert.Contains(Guid("Assets/Scripts/Colony/CampEventBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "guid: (\\w+), type: 2")) listed.Add(m.Groups[1].Value);
            Assert.AreEqual(CampEventTable.Code.Length, listed.Count);
            string script = Guid("Assets/Scripts/Colony/CampEventDefinition.cs.meta");
            for (int i = 0; i < CampEventTable.Code.Length; i++)
            {
                var row = CampEventTable.Code[i];
                string path = "Assets/Data/CampEvents/" + row.Id + ".asset";
                Assert.AreEqual(listed[i], Guid(path + ".meta"), path + " is listed out of order");
                string asset = Read(path);
                StringAssert.Contains(script, asset, path);
                Assert.AreEqual(row.Id, Field(asset, "id"));
                Assert.AreEqual(row.Weight.ToString(), Field(asset, "weight"), path);
                Assert.AreEqual(row.MinDay.ToString(), Field(asset, "minDay"), path);
                Assert.AreEqual(row.Module, Field(asset, "module"), path);
                Assert.AreEqual(row.NeedsRain ? "1" : "0", Field(asset, "needsRain"), path);
                Assert.AreEqual(row.NeedsRoom ? "1" : "0", Field(asset, "needsRoom"), path);
                Assert.AreEqual(row.NeedsFeud ? "1" : "0", Field(asset, "needsFeud"), path);
                Assert.AreEqual(row.NeedsHealthy ? "1" : "0", Field(asset, "needsHealthy"), path);
                Assert.AreEqual(row.NeedsMerchantAway ? "1" : "0", Field(asset, "needsMerchantAway"), path);
                Assert.AreEqual(row.Key, Field(asset, "titleKey"), path);
                Assert.AreEqual(row.Fallback, Field(asset, "fallback"), path);
            }
        }

        [Test]
        public void EveryEventReadsInBothLanguages()
        {
            foreach (var row in CampEventTable.Code)
            {
                Assert.AreEqual(row.Fallback, Loc.Raw(row.Key, "en"), row.Key);
                Assert.AreNotEqual(row.Key, Loc.Raw(row.Key, "es"), row.Key);
            }
            foreach (string key in new[] { "event.take", "event.turn", "event.repair", "event.repaired", "event.dawn", "event.joined", "event.left" })
                Assert.AreNotEqual(key, Loc.Raw(key, "es"), key);
        }
    }
}

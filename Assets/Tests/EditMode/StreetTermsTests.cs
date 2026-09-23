using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Expedition;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-31: the expedition's day timer and overtime, its own difficulty, and authored extraction points.</summary>
    public class StreetTermsTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [TearDown]
        public void Clear()
        {
            StreetTerms.Clear();
        }

        private static void Use(string district, StreetTerms.Terms terms)
        {
            StreetTerms.Use(new[] { new KeyValuePair<string, StreetTerms.Terms>(district, terms) });
        }

        [Test]
        public void AStreetRunsTenMinutesThenGoesIntoOvertime()
        {
            var plain = StreetTerms.For("nowhere");
            Assert.AreEqual(600f, plain.Duration, 0.001f);
            Assert.AreEqual(600f, StreetTerms.Left(plain, 0f), 0.001f);
            Assert.AreEqual(12f, StreetTerms.Left(plain, 588f), 0.001f);
            Assert.AreEqual(0f, StreetTerms.Left(plain, 700f), 0.001f);
            Assert.IsFalse(StreetTerms.Overtime(plain, 599.9f));
            Assert.IsTrue(StreetTerms.Overtime(plain, 600f));
            var open = new StreetTerms.Terms { Duration = 0f };
            Assert.AreEqual(-1f, StreetTerms.Left(open, 50f), "no limit");
            Assert.IsFalse(StreetTerms.Overtime(open, 99999f));
        }

        [Test]
        public void AnExpeditionCanOverrideTheRunsDifficulty()
        {
            Use("old_hospital", new StreetTerms.Terms { Difficulty = 3 });
            Assert.AreEqual(3, StreetTerms.Difficulty("old_hospital", 1));
            Assert.AreEqual(1, StreetTerms.Difficulty("ash_market", 1));
            Use("old_hospital", new StreetTerms.Terms { Difficulty = 0 });
            Assert.AreEqual(2, StreetTerms.Difficulty("old_hospital", 2), "0 keeps the run's");
            StreetTerms.Begin("old_hospital");
            Assert.AreSame(StreetTerms.For("old_hospital"), StreetTerms.Active);
            StreetTerms.Clear();
            Assert.AreEqual(600f, StreetTerms.Active.Duration, 0.001f);
        }

        [Test]
        public void AuthoredExtractionPointsArePickedByTheSeed()
        {
            Assert.IsFalse(StreetTerms.Extraction("ash_market", 7, out _, out _), "no points keeps the seeded gate");
            Use("ash_market", new StreetTerms.Terms { ExtractX = new[] { 1f, 2f, 3f }, ExtractZ = new[] { -1f, -2f, -3f } });
            Assert.IsTrue(StreetTerms.Extraction("ash_market", 7, out float x, out float z));
            Assert.AreEqual(2f, x);
            Assert.AreEqual(-2f, z);
            StreetTerms.Extraction("ash_market", 7, out float again, out _);
            Assert.AreEqual(x, again, "the same seed, the same gate");
            Assert.IsTrue(StreetTerms.Extraction("ash_market", -5, out _, out _), "negative seeds stay in range");
        }

        [Test]
        public void TheClockTailReadsTimeLeftThenOvertime()
        {
            Assert.AreEqual("7:05 left", StreetTerms.Line(425, true, "en"));
            Assert.AreEqual("quedan 0:09", StreetTerms.Line(9, true, "es"));
            Assert.AreEqual("Overtime", StreetTerms.Line(0, true, "en"));
            Assert.AreEqual("Tiempo extra", StreetTerms.Line(0, true, "es"));
            Assert.AreEqual("", StreetTerms.Line(40, false, "en"));
        }

        [Test]
        public void TheExpeditionAssetsCarryTheirTermsAndTheGameReadsThem()
        {
            StringAssert.Contains("  duration: 600\n  difficulty: 0\n  extractionPoints: []", Read("Assets/Data/Expeditions/ash_market.asset"));
            StringAssert.Contains("  duration: 480\n", Read("Assets/Data/Expeditions/old_hospital.asset"));
            StringAssert.Contains("StreetTerms.Use(terms);", Read("Assets/Scripts/Expedition/ExpeditionBook.cs"));
            string map = Read("Assets/Scripts/Shell/WorldMapService.cs");
            StringAssert.Contains("StreetTerms.Begin(districtId);", map);
            StringAssert.Contains("ApplyOpening(tension, interval, prefer, streetDifficulty,", map);
            StringAssert.Contains("StreetTerms.Extraction(districtId, seed,", Read("Assets/Scripts/Expedition/DistrictDressing.cs"));
            string director = Read("Assets/Scripts/AI/HordeDirector.cs");
            StringAssert.Contains("StreetTerms.Overtime(StreetTerms.Active, GameManager.Instance.ExpeditionTime)", director);
            StringAssert.Contains("tension = 100f;", director);
            StringAssert.Contains("StreetTerms.Line(shownLeft, true, null)", Read("Assets/Scripts/UI/HudController.cs"));
        }
    }
}

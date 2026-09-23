using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class StringTableTests
    {
        [Test]
        public void SpanishCoversEveryEnglishKey()
        {
            var missing = Loc.MissingIn("es");
            Assert.IsEmpty(missing, missing.Count > 0 ? "first missing: " + missing[0] : "");
        }

        [Test]
        public void ExportHasOneRowPerKeyWithBothLanguages()
        {
            var rows = LocCsv.Parse(LocCsv.Export());
            Assert.AreEqual(LocCsv.Header, string.Join(",", rows[0]));
            var keys = new List<string>(Loc.Keys);
            Assert.AreEqual(keys.Count + 1, rows.Count);
            for (int i = 1; i < rows.Count; i++)
            {
                Assert.AreEqual(3, rows[i].Length, rows[i][0]);
                Assert.AreEqual(Loc.Raw(rows[i][0], "en"), rows[i][1], rows[i][0]);
                Assert.AreEqual(Loc.Raw(rows[i][0], "es"), rows[i][2], rows[i][0]);
            }
        }

        [Test]
        public void FieldsWithCommasQuotesAndBreaksRoundTrip()
        {
            string[] tricky = { "plain", "a, b", "say \"hi\"", "two\nlines", "", "ñandú, \"é\"" };
            var parts = new List<string>();
            foreach (var value in tricky) parts.Add(LocCsv.Field(value));
            var rows = LocCsv.Parse(string.Join(",", parts) + "\r\nnext,row\n");
            Assert.AreEqual(2, rows.Count);
            CollectionAssert.AreEqual(tricky, rows[0]);
            CollectionAssert.AreEqual(new[] { "next", "row" }, rows[1]);
        }

        [Test]
        public void ExportIsSortedSoDiffsStayReadable()
        {
            var rows = LocCsv.Parse(LocCsv.Export());
            for (int i = 2; i < rows.Count; i++)
                Assert.Less(string.CompareOrdinal(rows[i - 1][0], rows[i][0]), 0, rows[i][0]);
        }

        private const string RussianSheet =
            "key,en,es,ru\n" +
            "@name,English,Español,Русский\n" +
            "hud.health,Health,Salud,Здоровье\n" +
            "hint.reload,x,x,\"Магазин пуст. Перезарядка: {key:Reload}\"\n";

        [Test]
        public void ATranslatorColumnBecomesALanguageThatFallsBackToEnglish()
        {
            try
            {
                var problems = new List<string>();
                var packs = LocCsv.Import(RussianSheet, problems);
                Assert.IsEmpty(problems, problems.Count > 0 ? problems[0] : "");
                Assert.AreEqual(1, packs.Count);
                Assert.AreEqual("ru", packs[0].Code);
                Assert.AreEqual("Русский", packs[0].Name);
                Assert.AreEqual(1, LocPacks.Register(packs));

                Assert.AreEqual("Здоровье", Loc.T("hud.health", "ru"));
                StringAssert.StartsWith("Магазин пуст.", Loc.Raw("hint.reload", "ru"));
                StringAssert.Contains("{key:Reload}", Loc.Raw("hint.reload", "ru"));
                Assert.AreEqual(Loc.Raw("codex.replay", "en"), Loc.Raw("codex.replay", "ru"));
                Assert.AreEqual("Salud", Loc.T("hud.health", "es"));

                CollectionAssert.Contains(PseudoLoc.Languages(false), "ru");
                Assert.AreEqual("ru", PseudoLoc.Next("es", false));
                Assert.AreEqual("en", PseudoLoc.Next("ru", false));
                Assert.AreEqual(PseudoLoc.Code, PseudoLoc.Next("ru", true));
                Assert.AreEqual("ru", PseudoLoc.Keep("ru", false));
                StringAssert.Contains("Русский", PseudoLoc.Label("ru"));
                Assert.IsTrue(FontChain.Needed("ru"));
            }
            finally
            {
                Loc.ClearPacks();
            }
            Assert.AreEqual("en", PseudoLoc.Keep("ru", false));
            Assert.AreEqual(Loc.Raw("hud.health", "en"), Loc.Raw("hud.health", "ru"));
        }

        [Test]
        public void ImportDropsLinesThatWouldBreakTheGame()
        {
            string sheet =
                "key,en,ja,english,ko\n" +
                "hud.health,Health,体力,Health,\n" +
                "no.such.key,x,x,x,x\n" +
                "hint.reload,x,弾切れ,x,탄창이 비었습니다 {key:Fire}\n";
            var problems = new List<string>();
            var packs = LocCsv.Import(sheet, problems);
            Assert.AreEqual(2, packs.Count);
            var ja = packs[0];
            var ko = packs[1];
            Assert.AreEqual("体力", ja.Lines["hud.health"]);
            Assert.IsFalse(ja.Lines.ContainsKey("hint.reload"), "a line that drops {key:Reload} is refused");
            Assert.IsFalse(ko.Lines.ContainsKey("hint.reload"), "a line that swaps the token is refused");
            Assert.AreEqual(0, ko.Lines.Count);
            Assert.IsTrue(problems.Exists(p => p.Contains("english")), "a bad code is reported");
            Assert.IsTrue(problems.Exists(p => p.Contains("no.such.key")));
            Assert.AreEqual(2, problems.FindAll(p => p.Contains("hint.reload")).Count);
            Assert.AreEqual(1, LocPacks.Register(packs), "an empty pack is not offered");
            Loc.ClearPacks();

            Assert.IsEmpty(LocCsv.Import("en,key\nx,y\n", problems));
            Assert.IsTrue(LocCsv.ValidCode("pt-br"));
            Assert.IsTrue(LocCsv.ValidCode("zh-hans"));
            Assert.IsFalse(LocCsv.ValidCode("en"));
            Assert.IsFalse(LocCsv.ValidCode(PseudoLoc.Code));
            Assert.IsFalse(LocCsv.ValidCode("r"));
            Assert.IsFalse(LocCsv.ValidCode("ru-"));
        }

        [Test]
        public void ALoadedLanguageExportsAndReimportsUnchanged()
        {
            try
            {
                LocPacks.Register(LocCsv.Import(RussianSheet, null));
                string sheet = LocCsv.Export();
                StringAssert.StartsWith(LocCsv.Header + ",ru\n" + LocCsv.NameRow + ",", sheet);
                Loc.ClearPacks();
                var again = LocCsv.Import(sheet, null);
                Assert.AreEqual(1, again.Count);
                Assert.AreEqual("Русский", again[0].Name);
                Assert.AreEqual(2, again[0].Lines.Count);
                Assert.AreEqual("Здоровье", again[0].Lines["hud.health"]);
            }
            finally
            {
                Loc.ClearPacks();
            }
        }

        [Test]
        public void EveryCsvInTheFolderLoadsInNameOrder()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "oz-loc-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "a.csv"), RussianSheet);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "b.csv"), "key,en,ru,uk\nhud.health,Health,Жизнь,Здоров'я\n");
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "notes.txt"), "ignored");
                var problems = new List<string>();
                Assert.AreEqual(3, LocPacks.Load(dir, problems));
                CollectionAssert.AreEqual(new[] { "ru", "uk" }, Loc.Packs);
                Assert.AreEqual("Жизнь", Loc.T("hud.health", "ru"), "the later file replaces the code");
                Assert.AreEqual(0, LocPacks.Load(System.IO.Path.Combine(dir, "missing"), problems));
            }
            finally
            {
                Loc.ClearPacks();
                System.IO.Directory.Delete(dir, true);
            }
        }

        [Test]
        public void EachLanguageNamesTheScriptItNeeds()
        {
            Assert.AreEqual(TextScript.Latin, FontChain.ScriptOf("en"));
            Assert.AreEqual(TextScript.Latin, FontChain.ScriptOf("es"));
            Assert.AreEqual(TextScript.Latin, FontChain.ScriptOf(PseudoLoc.Code));
            Assert.AreEqual(TextScript.Latin, FontChain.ScriptOf(null));
            Assert.AreEqual(TextScript.Cyrillic, FontChain.ScriptOf("ru"));
            Assert.AreEqual(TextScript.Cyrillic, FontChain.ScriptOf("uk-UA"));
            Assert.AreEqual(TextScript.Cjk, FontChain.ScriptOf("zh-Hans"));
            Assert.AreEqual(TextScript.Cjk, FontChain.ScriptOf("JA"));
            Assert.AreEqual(TextScript.Cjk, FontChain.ScriptOf("ko_KR"));
            foreach (var code in PseudoLoc.Languages(true))
                Assert.IsFalse(FontChain.Needed(code), code + " should keep the default font");
            Assert.IsTrue(FontChain.Needed("ru"));
        }

        [Test]
        public void TheChainLeadsWithTheLanguageScriptAndCoversLatinCyrillicAndCjk()
        {
            foreach (var code in new[] { "en", "ru", "zh", "ja", "ko" })
            {
                var chain = FontChain.Scripts(code);
                Assert.AreEqual(FontChain.ScriptOf(code), chain[0], code);
                CollectionAssert.AreEquivalent(FontChain.Order, chain, code);
            }
            foreach (var script in FontChain.Order)
            {
                Assert.IsNotEmpty(FontChain.Families(script, "en"), script.ToString());
                Assert.IsNotEmpty(FontChain.Probe(script, "en"), script.ToString());
            }
            Assert.AreNotEqual(FontChain.Families(TextScript.Cjk, "ja")[0], FontChain.Families(TextScript.Cjk, "zh")[0]);
            Assert.AreNotEqual(FontChain.Families(TextScript.Cjk, "ko")[0], FontChain.Families(TextScript.Cjk, "zh")[0]);
            StringAssert.Contains("あ", FontChain.Probe(TextScript.Cjk, "ja"));
            StringAssert.Contains("한", FontChain.Probe(TextScript.Cjk, "ko"));
        }

        [Test]
        public void PickTakesTheFirstPreferredFamilyThatIsInstalled()
        {
            var installed = new[] { "arial", "Meiryo", "DejaVu Sans" };
            Assert.AreEqual("Arial", FontChain.Pick(installed, FontChain.Families(TextScript.Latin, "en")));
            Assert.AreEqual("Meiryo", FontChain.Pick(installed, FontChain.Families(TextScript.Cjk, "ja")));
            Assert.AreEqual("", FontChain.Pick(installed, FontChain.Families(TextScript.Cjk, "ko")));
            Assert.AreEqual("", FontChain.Pick(null, FontChain.Families(TextScript.Latin, "en")));
        }
    }
}

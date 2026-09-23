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

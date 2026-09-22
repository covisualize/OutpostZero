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
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.AI;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-29: throwables as data, the held-key arc preview, and the stealth acceptance numbers.</summary>
    public class ThrowableBookTests
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
            ThrowableTable.Clear();
        }

        [Test]
        public void FiveThrowableAssetsMatchTheBuiltInRowsInBookOrder()
        {
            string book = Read("Assets/Resources/ThrowableBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Player/ThrowableBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);
            var rows = ThrowableTable.BuiltInRows();
            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual(rows.Count, listed.Count);
            string script = Guid("Assets/Scripts/Player/ThrowableDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string path = "Assets/Data/Throwables/" + row.Id + ".asset";
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Kind.ToString(), f["kind"], row.Id);
                Assert.AreEqual(row.Noise, F(f, "noise"), 0.0001f, row.Id);
                Assert.AreEqual(row.Fuse, F(f, "fuse"), 0.0001f, row.Id);
                Assert.AreEqual(row.Radius, F(f, "radius"), 0.0001f, row.Id);
                Assert.AreEqual(row.Damage, F(f, "damage"), 0.0001f, row.Id);
                Assert.AreEqual(row.Seconds, F(f, "seconds"), 0.0001f, row.Id);
                Assert.AreEqual(row.Size, F(f, "size"), 0.0001f, row.Id);
                Assert.IsTrue(File.Exists(Path.Combine(Root, "Assets", "Data", "Items", row.Id + ".asset")), row.Id + " is not a pack item");
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Throwables"), "*.asset").Length, "no stray throwable assets");
            StringAssert.Contains("ThrowableBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }

        [Test]
        public void TheRowsKeepTheIssuesNumbers()
        {
            Assert.AreEqual(18f, ThrowableTable.Of("street_bottle").Noise, 0.0001f, "a bottle breaks loud enough to pull a group 15 m off");
            Assert.AreEqual(TossKind.Lure, ThrowableTable.Of("street_bottle").Kind);
            Assert.AreEqual(20f, ThrowableTable.Of("flare").Seconds, 0.0001f, "a flare calls for 20 s");
            Assert.AreEqual(TossKind.Flare, ThrowableTable.Of("flare").Kind);
            Assert.AreEqual(TossKind.Bomb, ThrowableTable.Of("pipe_bomb").Kind);
            Assert.AreEqual(PipeBlast.Radius, ThrowableTable.Of("pipe_bomb").Radius, 0.0001f);
            Assert.AreEqual(PipeBlast.Damage, ThrowableTable.Of("pipe_bomb").Damage, 0.0001f);
            Assert.AreEqual(TossKind.Fire, ThrowableTable.Of("molotov").Kind);
            Assert.IsNull(ThrowableTable.Of("medkit"));
            Assert.IsNull(ThrowableTable.Of(null));
        }

        [Test]
        public void AnAssetOverridesTheCodeAndANewIdThrows()
        {
            ThrowableTable.Use(new List<ThrowableTable.Row>
            {
                new ThrowableTable.Row { Id = "flare", Kind = TossKind.Flare, Noise = 20f, Fuse = 0.8f, Radius = 11f, Seconds = 30f },
                new ThrowableTable.Row { Id = "cell", Kind = TossKind.Lure, Noise = 10f, Fuse = 0.6f },
                new ThrowableTable.Row { Id = "cell", Kind = TossKind.Bomb },
                new ThrowableTable.Row { Id = "water", Kind = TossKind.None }
            });
            Assert.IsTrue(ThrowableTable.FromAsset);
            Assert.AreEqual(30f, ThrowableTable.Of("flare").Seconds, 0.0001f);
            Assert.AreEqual(TossKind.Lure, TossKind.Of("cell"), "the first row for an id wins");
            Assert.IsFalse(TossKind.Throws("water"), "a row that throws nothing is dropped");
            Assert.AreEqual(TossKind.Bomb, TossKind.Of("pipe_bomb"), "ids the book lacks fall back to the code");
            Assert.AreEqual(2, ThrowableTable.All.Count, "the throw key tries only the book's rows, in its order");
            Assert.AreEqual("flare", ThrowableTable.All[0].Id);
        }

        [Test]
        public void TheArcStartsAtTheHandAndLandsWhereTheThrowDoes()
        {
            var along = new float[ThrowPreview.Points];
            var up = new float[ThrowPreview.Points];
            int count = ThrowArc.Sample(ThrowArc.Height, ThrowArc.Forward, ThrowArc.Lift, ThrowArc.Gravity, along, up);
            Assert.AreEqual(ThrowPreview.Points, count);
            Assert.AreEqual(0f, along[0], 0.0001f);
            Assert.AreEqual(ThrowArc.Height, up[0], 0.0001f);
            float landing = ThrowArc.Flight(ThrowArc.Height, ThrowArc.Forward, ThrowArc.Lift, ThrowArc.Gravity);
            Assert.AreEqual(landing, along[count - 1], 0.01f);
            Assert.AreEqual(0f, up[count - 1], 0.01f);
            Assert.Greater(landing, 15f, "the preview reaches the issue's 15 m bottle throw");
            float peak = 0f;
            for (int i = 0; i < count; i++) if (up[i] > peak) peak = up[i];
            Assert.Greater(peak, ThrowArc.Height, "the arc rises before it falls");
            for (int i = 1; i < count; i++) Assert.Greater(along[i], along[i - 1]);
            Assert.AreEqual(0, ThrowArc.Sample(1f, 1f, 1f, 9.81f, new float[1], new float[1]));
        }

        [Test]
        public void HoldingTheThrowKeyAimsAndReleasingThrows()
        {
            string hands = Read("Assets/Scripts/Player/PlayerInteractor.cs");
            StringAssert.Contains("ExpeditionInput.ThrowReleased", hands);
            StringAssert.Contains("ExpeditionInput.ThrowHeld && NextThrowable() != null", hands);
            StringAssert.Contains("arc?.Show(transform.position, transform.forward)", hands);
            StringAssert.Contains("ThrowableBook.Ensure();", hands);
            StringAssert.Contains("ThrowableTable.Of(id)", hands);
            StringAssert.DoesNotContain("ThrowId(\"molotov\")", hands, "the pick order comes from the book");
            StringAssert.DoesNotContain("PipeBlast.Damage", hands, "blast damage comes from the row");
            string input = Read("Assets/Scripts/Player/ExpeditionInput.cs");
            StringAssert.Contains("InputPhase.Released", input);
        }

        [Test]
        public void ACrouchedUnlitLeaderBehindABarrierIsUnseenPastFourMetres()
        {
            float cover = CoverSight.Scale(0f, -1f, 0f, 4f, 0f, 0f, 0f, true);
            foreach (float sight in new[] { 12f, 14f, 16f })
            {
                foreach (float night in new[] { 0f, 1f })
                {
                    float exposure = SpotRange.Exposure(true, false, false, night, 0f);
                    Assert.Less(SpotRange.Meters(sight, exposure, true, 1f, cover), 4f, "sight " + sight + ", night " + night);
                }
                float lit = SpotRange.Exposure(true, false, true, 1f, 0f);
                Assert.IsTrue(SpotRange.Notices(5f, sight, lit, true, 1f, 1f, 0f, 110f), "a flashlight at 5 m is spotted, sight " + sight);
            }
        }
    }
}

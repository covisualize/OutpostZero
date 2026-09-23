using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Sensory;

namespace OutpostZero.Tests.EditMode
{
    public class NoiseBookTests
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
            NoiseTable.Clear();
        }

        [Test]
        public void EveryNoiseAssetMatchesItsBuiltInRowInBookOrder()
        {
            string book = Read("Assets/Resources/NoiseBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Sensory/NoiseBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);
            var rows = NoiseTable.BuiltInRows();
            Assert.AreEqual(22, rows.Count);
            Assert.AreEqual(rows.Count, listed.Count);
            string script = Guid("Assets/Scripts/Sensory/NoiseDefinition.cs.meta");
            var seen = new HashSet<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                Assert.IsTrue(seen.Add(row.Id), row.Id + " is listed twice");
                string path = "Assets/Data/Noise/" + row.Id + ".asset";
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Id, f["m_Name"]);
                Assert.AreEqual(row.Radius, F(f, "radius"), 0.0001f, row.Id);
                Assert.AreEqual(row.Loud, F(f, "loudness"), 0.0001f, row.Id);
                Assert.Greater(row.Radius, 0f, row.Id);
                Assert.That(row.Loud, Is.InRange(0f, 1f), row.Id);
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Noise"), "*.asset").Length, "no stray noise assets");
            StringAssert.Contains("NoiseBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
            StringAssert.Contains("NoiseBook.Ensure();", Read("Assets/Scripts/Sensory/NoiseManager.cs"));
        }

        [Test]
        public void TheBuiltInRowsKeepTheShippedNumbers()
        {
            Assert.AreEqual(OutpostZero.Expedition.DoorCreak.Radius, NoiseTable.Radius(NoiseTable.DoorCreak));
            Assert.AreEqual(OutpostZero.Expedition.DoorBar.BreakNoise, NoiseTable.Radius(NoiseTable.DoorBreak));
            Assert.AreEqual(OutpostZero.Combat.BarrelBlast.Noise, NoiseTable.Radius(NoiseTable.BarrelBlast));
            Assert.AreEqual(OutpostZero.Sensory.StormCover.Radius, NoiseTable.Radius(NoiseTable.Thunder));
            Assert.AreEqual(OutpostZero.Player.RationNoise.Loud, NoiseTable.Loud(NoiseTable.Ration), 0.0001f);
            Assert.AreEqual(13f, NoiseTable.Radius(NoiseTable.StepSprint));
            Assert.Greater(NoiseTable.Radius(NoiseTable.StepSprint), NoiseTable.Radius(NoiseTable.StepWalk));
            Assert.Greater(NoiseTable.Radius(NoiseTable.StepWalk), NoiseTable.Radius(NoiseTable.StepCrouch));
            Assert.AreEqual(0f, NoiseTable.Radius("no_such_noise"));
        }

        [Test]
        public void ABookRowOverridesAndTheFootstepFieldIsTheFallback()
        {
            Assert.AreEqual(7.5f, StepNoise.Base(7.5f, NoiseTable.StepWalk), "no book: the Inspector value stands");
            NoiseTable.Use(new List<NoiseTable.Row>
            {
                new NoiseTable.Row { Id = NoiseTable.Glass, Radius = 20f, Loud = 1.4f },
                new NoiseTable.Row { Id = NoiseTable.Glass, Radius = 1f, Loud = 0.1f },
                new NoiseTable.Row { Id = NoiseTable.StepWalk, Radius = 4f, Loud = 0.5f },
                null
            });
            Assert.IsTrue(NoiseTable.FromAsset);
            Assert.AreEqual(20f, NoiseTable.Radius(NoiseTable.Glass), "the first row for an id wins");
            Assert.AreEqual(1f, NoiseTable.Loud(NoiseTable.Glass), "loudness is clamped to 1");
            Assert.AreEqual(4f, StepNoise.Base(7.5f, NoiseTable.StepWalk));
            Assert.AreEqual(2f, StepNoise.Base(2f, NoiseTable.StepCrouch), "a row the book lacks leaves the field");
            Assert.AreEqual(OutpostZero.Expedition.DoorCreak.Radius, NoiseTable.Radius(NoiseTable.DoorCreak), "a row the book lacks falls back to code");
            Assert.AreEqual(10f, OutpostZero.Graphics.AshCough.Carry(false, 10f));
            Assert.AreEqual(5f, OutpostZero.Graphics.AshCough.Carry(true, 10f));
        }

        [Test]
        public void NoEmitCallHardCodesAWorldNoise()
        {
            var old = new Regex("^(\\d|DoorBar\\.|DoorCreak\\.|PaneGlass\\.|StraggleCall\\.|StreetAid\\.|(Sensory\\.)?StormCover\\.|BarrelBlast\\.|ShellRing\\.|(Sensory\\.)?BleedScent\\.|QuietKill\\.|LootTake\\.|RationNoise\\.|Noise$)");
            var call = new Regex("EmitNoise\\(([^,;]+),\\s*([^,;]+),\\s*([^,;]+),");
            int checkedCalls = 0;
            foreach (string file in Directory.GetFiles(Path.Combine(Root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name == "NoiseManager.cs" || name == "NoiseEmitter.cs") continue;
                string text = File.ReadAllText(file);
                foreach (Match m in call.Matches(text))
                {
                    string radius = m.Groups[2].Value.Trim();
                    Assert.IsFalse(old.IsMatch(radius), name + " emits a fixed radius: " + radius);
                    checkedCalls++;
                }
            }
            Assert.GreaterOrEqual(checkedCalls, 20);

            string steps = Read("Assets/Scripts/Player/PlayerController.cs");
            StringAssert.Contains("StepNoise.Base(walkNoiseRadius, NoiseTable.StepWalk)", steps);
            StringAssert.Contains("StepNoise.Base(sprintNoiseRadius, NoiseTable.StepSprint)", steps);
            StringAssert.Contains("StepNoise.Base(crouchNoiseRadius, NoiseTable.StepCrouch)", steps);
            StringAssert.Contains("NoiseTable.Loud(stepRow)", steps);
            StringAssert.Contains("AshCough.Carry(IsCrouching, NoiseTable.Radius(NoiseTable.Cough))", steps);
            StringAssert.Contains("BladeClang.Radius(body != null && body.IsCrouching, OutpostZero.Sensory.NoiseTable.Radius(OutpostZero.Sensory.NoiseTable.BladeClang))", Read("Assets/Scripts/Combat/MeleeWeapon.cs"));
            Assert.AreEqual(OutpostZero.Combat.BladeClang.Reach, NoiseTable.Radius(NoiseTable.BladeClang));
        }
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class StatusBookTests
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
            StatusTable.Clear();
        }

        [Test]
        public void SixStatusAssetsMatchTheBuiltInRowsInBookOrder()
        {
            string book = Read("Assets/Resources/StatusBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Player/StatusBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);
            var rows = StatusTable.BuiltInRows();
            Assert.AreEqual(6, rows.Count);
            Assert.AreEqual(rows.Count, listed.Count);
            string script = Guid("Assets/Scripts/Player/StatusEffectDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                Assert.AreEqual(i + 1, (int)row.Kind, "rows follow the enum");
                string path = "Assets/Data/Status/" + row.Id + ".asset";
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(((int)row.Kind).ToString(), f["kind"], row.Id);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.LabelKey, f["labelKey"]);
                Assert.AreEqual(row.DamagePerSecond, F(f, "damagePerSecond"), 0.0001f, row.Id);
                Assert.AreEqual(row.Seconds, F(f, "seconds"), 0.0001f, row.Id);
                Assert.AreEqual(row.Scale, F(f, "scale"), 0.0001f, row.Id);
                Assert.IsTrue(Loc.Has(row.LabelKey, "en"), row.LabelKey + " has no English");
                Assert.IsTrue(Loc.Has(row.LabelKey, "es"), row.LabelKey + " has no Spanish");
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Status"), "*.asset").Length, "no stray status assets");
            StringAssert.Contains("StatusBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }

        [Test]
        public void TheRowsKeepTheIssuesNumbers()
        {
            Assert.AreEqual(1f, StatusTable.Of(StatusKind.Bleeding).DamagePerSecond, 0.0001f);
            Assert.Less(StatusTable.Of(StatusKind.Bleeding).Seconds, 0f, "a bleed runs until bandaged");
            Assert.AreEqual(4f, StatusTable.Of(StatusKind.Poisoned).DamagePerSecond, 0.0001f);
            Assert.AreEqual(6f, StatusTable.Of(StatusKind.Poisoned).Seconds, 0.0001f);
            Assert.AreEqual(Affliction.AdrenalineSprint, StatusTable.Of(StatusKind.Adrenaline).Scale, 0.0001f);
            Assert.AreEqual(0.55f, StatusTable.Of(StatusKind.Slowed).Scale, 0.0001f);
            Assert.AreEqual(6f, StatusTable.SecondsFor(StatusKind.Poisoned, 0f), 0.0001f);
            Assert.AreEqual(3f, StatusTable.SecondsFor(StatusKind.Poisoned, 3f), 0.0001f, "the source's own length wins");
            Assert.AreEqual(90f, 100f / StatusTable.Of(StatusKind.Bleeding).DamagePerSecond, 10f, "untreated bleeding kills in about 90 s");
        }

        [Test]
        public void ALoadedBookOverridesTheCodeRows()
        {
            StatusTable.Use(new[] { new StatusTable.Row { Kind = StatusKind.Poisoned, Id = "poisoned", DamagePerSecond = 9f, Seconds = 2f } });
            Assert.IsTrue(StatusTable.FromAsset);
            Assert.AreEqual(9f, StatusTable.Of(StatusKind.Poisoned).DamagePerSecond, 0.0001f);
            Assert.AreEqual(1f, StatusTable.Of(StatusKind.Bleeding).DamagePerSecond, 0.0001f, "kinds the book lacks keep the code row");
            StatusTable.Clear();
            Assert.AreEqual(4f, StatusTable.Of(StatusKind.Poisoned).DamagePerSecond, 0.0001f);
        }

        [Test]
        public void ZombieAssetsNameTheirHitEffectsAndMatchTheBuiltInSets()
        {
            var abilities = new Dictionary<string, ZombieSpecialAbility>
            {
                { "Walker", ZombieSpecialAbility.None },
                { "Runner", ZombieSpecialAbility.Lunge },
                { "Brute", ZombieSpecialAbility.Charge }
            };
            foreach (var pair in abilities)
            {
                string text = Read("Assets/Data/Zombies/" + pair.Key + ".asset");
                var kinds = Regex.Matches(text, "^  - kind: (\\d+)\\n    chance: ([\\d.]+)\\n    seconds: ([\\d.]+)$", RegexOptions.Multiline);
                var expected = HitEffects.Default(pair.Value);
                Assert.AreEqual(expected.Length, kinds.Count, pair.Key);
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.AreEqual(((int)expected[i].kind).ToString(), kinds[i].Groups[1].Value, pair.Key);
                    Assert.AreEqual(expected[i].chance, float.Parse(kinds[i].Groups[2].Value, CultureInfo.InvariantCulture), 0.0001f, pair.Key);
                    Assert.AreEqual(expected[i].seconds, float.Parse(kinds[i].Groups[3].Value, CultureInfo.InvariantCulture), 0.0001f, pair.Key);
                }
            }
            string ai = Read("Assets/Scripts/AI/ZombieAI.cs");
            StringAssert.Contains("HitEffects.For(archetype.onHitEffects, archetype.specialAbility)", ai);
            StringAssert.Contains("effects.Apply(landed[i].kind, landed[i].seconds)", ai);
            Assert.IsFalse(ai.Contains("ClawCut."), "hits roll through the data, not the old constants");
        }

        [Test]
        public void HitEffectsRollOnTheirOwnOdds()
        {
            var runner = HitEffects.Default(ZombieSpecialAbility.Lunge);
            Assert.AreEqual(StatusKind.Bleeding, runner[0].kind);
            Assert.IsTrue(runner[0].Lands(0.34f));
            Assert.IsFalse(runner[0].Lands(0.35f), "35% from a runner claw");
            var brute = HitEffects.Default(ZombieSpecialAbility.Charge);
            Assert.IsTrue(brute[1].Lands(0.9999f), "a brute blow always knocks down");
            Assert.AreEqual(1, HitEffects.Default(ZombieSpecialAbility.None).Length, "a walker only infects");
            Assert.IsFalse(new HitEffect(StatusKind.None, 1f, 1f).Lands(0f));
            var authored = new[] { new HitEffect(StatusKind.Poisoned, 0.5f, 3f) };
            Assert.AreSame(authored, HitEffects.For(authored, ZombieSpecialAbility.Lunge));
            Assert.AreEqual(2, HitEffects.For(new HitEffect[0], ZombieSpecialAbility.Lunge).Length);
            Assert.AreEqual(2, HitEffects.For(null, ZombieSpecialAbility.Charge).Length);
        }

        [Test]
        public void MedicalItemsTreatThroughTheirUseEffect()
        {
            Assert.IsTrue(UseEffects.Treats(ItemCatalog.Find("bandage").Effect, UseEffect.StopBleeding));
            Assert.IsTrue(UseEffects.Treats(ItemCatalog.Find("medkit").Effect, UseEffect.StopBleeding));
            Assert.IsTrue(UseEffects.Treats(ItemCatalog.Find("antibiotics").Effect, UseEffect.CureInfection));
            Assert.IsTrue(UseEffects.Treats(ItemCatalog.Find("painkillers").Effect, UseEffect.PainRelief));
            Assert.IsFalse(UseEffects.Treats(ItemCatalog.Find("water").Effect, UseEffect.StopBleeding));
            Assert.IsFalse(UseEffects.Treats(UseEffect.StopBleeding, UseEffect.None));
            var expected = new Dictionary<string, int> { { "medkit", 3 }, { "bandage", 1 }, { "antibiotics", 2 }, { "painkillers", 4 } };
            foreach (var file in Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Items"), "*.asset"))
            {
                var f = Fields(File.ReadAllText(file).Replace("\r\n", "\n"));
                if (!f.ContainsKey("use")) continue;
                Assert.IsTrue(f.ContainsKey("useEffect"), Path.GetFileName(file) + " has no useEffect");
                int want = expected.TryGetValue(f["id"], out int v) ? v : 0;
                Assert.AreEqual(want.ToString(), f["useEffect"], f["id"]);
                Assert.AreEqual(want, (int)ItemCatalog.Find(f["id"]).Effect, f["id"] + " asset and code disagree");
            }
            string pack = Read("Assets/Scripts/Player/PlayerInventory.cs");
            Assert.IsFalse(pack.Contains("record.Id == \"bandage\""), "the bandage path reads its use effect");
            Assert.IsFalse(pack.Contains("record.Id == \"antibiotics\""));
            Assert.IsFalse(pack.Contains("record.Id == \"painkillers\""));
        }

        [Test]
        public void PillsCountDownInMinutesAndSeconds()
        {
            Assert.AreEqual("Poison 0:06", StatusTimer.Pill("Poison", 5.2f));
            Assert.AreEqual("Poison", StatusTimer.Pill("Poison", 0f));
            Assert.AreEqual("1:30", StatusTimer.Clock(90));
            Assert.AreEqual(1, StatusTimer.Shown(0.01f), "never 0:00 while it lasts");
            Assert.AreEqual(89.95f, StatusTimer.ToNextStage(0.05f), 0.001f);
            Assert.AreEqual(Affliction.StageTwo - 100f, StatusTimer.ToNextStage(100f), 0.001f);
            Assert.AreEqual(0f, StatusTimer.ToNextStage(0f));
            Assert.AreEqual(0f, StatusTimer.ToNextStage(Affliction.StageTwo + 0.5f), "the last stage has no next");
            string hud = Read("Assets/Scripts/UI/HudController.cs");
            StringAssert.Contains("StatusTimer.Pill(Loc.T(\"hud.poison\"), poison)", hud);
            StringAssert.Contains("StatusTimer.Pill(StreetHud.Infection(stage, null), turn)", hud);
            StringAssert.Contains("StatusTimer.Pill(Loc.T(\"hud.adrenaline\"), rush)", hud);
        }
    }
}

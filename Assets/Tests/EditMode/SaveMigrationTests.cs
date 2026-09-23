using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class SaveMigrationTests
    {
        static string Fixture(string name) =>
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Tests", "EditMode", "Fixtures", name);

        static SaveGameData VersionOne() => new SaveGameData
        {
            schemaVersion = 1,
            day = 6,
            hour = 26.5f,
            colonyScrap = 41,
            fuelSet = 0,
            survivors = new[]
            {
                new SurvivorSave { id = "mara", alive = true, leader = true, hunger = 40f, thirst = 35f, fatigue = 140f, fatigueKnown = 1, opinion = 5, injury = 1, needsTracked = true, task = "Heal", bond = "tomas" },
                new SurvivorSave { id = "tomas", alive = true, morale = 51f, task = "", bond = "mara", aside = null, past = null },
                null
            },
            modules = new[]
            {
                new ModuleSave { kind = "Barricade", integrity = 140, age = 3 },
                new ModuleSave { kind = "" },
                null
            }
        };

        [Test]
        public void VersionOneUpgradesToTheCurrentSchema()
        {
            Assert.AreEqual(3, SaveCodec.CurrentSchema);
            Assert.AreEqual(1, SaveMigrations.Oldest);
            Assert.IsTrue(SaveMigrations.CanUpgrade(1));
            Assert.IsTrue(SaveMigrations.CanUpgrade(SaveCodec.CurrentSchema));
            Assert.IsFalse(SaveMigrations.CanUpgrade(0));
            Assert.IsFalse(SaveMigrations.CanUpgrade(SaveCodec.CurrentSchema + 1));

            var data = SaveMigrations.Upgrade(VersionOne());
            Assert.AreEqual(SaveCodec.CurrentSchema, data.schemaVersion);
            Assert.AreEqual(6, data.day);
            Assert.AreEqual(2.5f, data.hour, 1e-4f);
            Assert.AreEqual(41, data.colonyScrap);
            Assert.AreEqual(1, data.fuelSet);
            Assert.AreEqual(FuelTank.Start, FuelTank.Unpack(data.fuel, data.fuelSet), 1e-4f);

            Assert.AreEqual(2, data.survivors.Length);
            var mara = data.survivors[0];
            Assert.AreEqual(40f, mara.hunger);
            Assert.AreEqual(100f, mara.fatigue);
            Assert.AreEqual(5, mara.opinion);
            Assert.AreEqual(1, mara.injury);
            var tomas = data.survivors[1];
            Assert.IsTrue(tomas.needsTracked);
            Assert.AreEqual(SaveMigrations.UntrackedNeed, tomas.hunger);
            Assert.AreEqual(SaveMigrations.UntrackedNeed, tomas.thirst);
            Assert.AreEqual(0, tomas.fatigueKnown);
            Assert.AreEqual(SaveMigrations.BondOpinion, tomas.opinion);
            Assert.AreEqual("Rest", tomas.task);
            Assert.AreEqual("", tomas.aside);
            Assert.AreEqual("", tomas.past);

            Assert.AreEqual(1, data.modules.Length);
            Assert.AreEqual("Barricade", data.modules[0].kind);
            Assert.AreEqual(100, data.modules[0].integrity);
        }

        [Test]
        public void UpgradingTwiceChangesNothing()
        {
            var once = SaveMigrations.Upgrade(VersionOne());
            var twice = SaveMigrations.Upgrade(SaveMigrations.Upgrade(VersionOne()));
            CollectionAssert.IsEmpty(SaveDiff.Compare(once, twice));
            var current = new SaveGameData { day = 3 };
            Assert.AreSame(current, SaveMigrations.Upgrade(current));
            Assert.AreEqual(3, current.day);
        }

        [Test]
        public void HoursWrapIntoOneDay()
        {
            Assert.AreEqual(0f, SaveMigrations.WrapHour(24f));
            Assert.AreEqual(23f, SaveMigrations.WrapHour(-1f));
            Assert.AreEqual(6.5f, SaveMigrations.WrapHour(6.5f));
            Assert.AreEqual(18.5f, SaveMigrations.WrapHour(float.NaN));
        }

        [Test]
        public void TheDiffNamesEveryFieldThatMoved()
        {
            var a = new SaveGameData { day = 4, survivors = new[] { new SurvivorSave { id = "a", morale = 10f } } };
            var b = new SaveGameData { day = 5, survivors = new[] { new SurvivorSave { id = "a", morale = 11f } } };
            CollectionAssert.AreEquivalent(new[] { "save.day", "save.survivors[0].morale" }, SaveDiff.Compare(a, b));
            b.day = 4;
            b.survivors = new[] { new SurvivorSave { id = "a", morale = 10f }, new SurvivorSave() };
            CollectionAssert.AreEqual(new[] { "save.survivors.Length" }, SaveDiff.Compare(a, b));
            CollectionAssert.IsEmpty(SaveDiff.Compare(new SurvivorSave { past = "" }, new SurvivorSave { past = null }));
        }

        [Test]
        public void TheCommittedVersionOneFixtureLoads()
        {
            string json = File.ReadAllText(Fixture("save_v1.json"));
            StringAssert.Contains("\"schemaVersion\": 1", json);
            Assert.IsTrue(SaveCodec.TryDeserialize(json, out var data, out var error), error);
            Assert.AreEqual(SaveCodec.CurrentSchema, data.schemaVersion);
            Assert.AreEqual(6, data.day);
            Assert.AreEqual(2.5f, data.hour, 1e-4f);
            Assert.AreEqual(1337, data.worldSeed);
            Assert.AreEqual("es", data.language);
            Assert.AreEqual(3, data.survivors.Length);
            Assert.AreEqual(SaveMigrations.UntrackedNeed, data.survivors[1].hunger);
            Assert.AreEqual("Rest", data.survivors[1].task);
            Assert.IsFalse(data.survivors[2].alive);
            Assert.AreEqual(2, data.modules.Length);
            Assert.AreEqual(100, data.modules[0].integrity);
            Assert.AreEqual("Generator", data.modules[1].kind);
        }

        [Test]
        public void TheSealIsCheckedAgainstTheFileTextSoOlderSavesStillOpen()
        {
            string bare = "{\n    \"day\": 4,\n    \"seal\": \"\",\n    \"food\": 9\n}";
            string seal = SaveSlots.Hash(bare);
            string sealed_ = bare.Replace("\"seal\": \"\"", "\"seal\": \"" + seal + "\"");
            Assert.IsTrue(SaveCodec.SealHolds(sealed_));
            Assert.IsTrue(SaveCodec.SealHolds(sealed_.Replace("\n", "\r\n")), "a CRLF copy of the file still holds");
            Assert.IsFalse(SaveCodec.SealHolds(sealed_.Replace("\"food\": 9", "\"food\": 99")), "an edited value breaks the seal");
            Assert.IsFalse(SaveCodec.SealHolds(bare), "an empty seal proves nothing");
            Assert.IsFalse(SaveCodec.SealHolds(""));
        }

        [Test]
        public void ASealedSaveFromABuildWithFewerFieldsStillLoads()
        {
            Assert.IsTrue(SaveCodec.TryDeserialize(File.ReadAllText(Fixture("save_v1.json")), out var data, out var error), error);
            string written = SaveCodec.Serialize(data);
            Assert.IsTrue(SaveCodec.SealHolds(written));
            var lines = new System.Collections.Generic.List<string>(written.Replace("\r\n", "\n").Split('\n'));
            int drop = lines.FindIndex(l => l.TrimStart().StartsWith("\"subtitles\""));
            Assert.Greater(drop, 0);
            lines.RemoveAt(drop);
            string older = string.Join("\n", lines);
            string bare = System.Text.RegularExpressions.Regex.Replace(older, "\"seal\": \"[0-9a-f]*\"", "\"seal\": \"\"");
            older = older.Replace("\"seal\": \"" + data.seal + "\"", "\"seal\": \"" + SaveSlots.Hash(bare) + "\"");
            Assert.IsTrue(SaveCodec.TryDeserialize(older, out var loaded, out error), error);
            Assert.AreEqual(data.day, loaded.day);
            Assert.IsFalse(SaveCodec.TryDeserialize(older.Replace("\"day\": " + data.day, "\"day\": 99"), out _, out error));
            Assert.AreEqual("seal", error);
        }

        [Test]
        public void ARoundTripIsDeepEqual()
        {
            Assert.IsTrue(SaveCodec.TryDeserialize(File.ReadAllText(Fixture("save_v1.json")), out var first, out var error), error);
            string written = SaveCodec.Serialize(first);
            Assert.IsTrue(SaveCodec.TryDeserialize(written, out var second, out error), error);
            CollectionAssert.IsEmpty(SaveDiff.Compare(first, second));
            Assert.AreEqual(written, SaveCodec.Serialize(second));
        }

        [Test]
        public void ManualSavesAreMadeInCampOnly()
        {
            Assert.IsTrue(SaveSlots.ManualAllowed(GameState.CampManagement, GameState.CampManagement));
            Assert.IsTrue(SaveSlots.ManualAllowed(GameState.Paused, GameState.CampManagement));
            Assert.IsFalse(SaveSlots.ManualAllowed(GameState.Paused, GameState.ExpeditionActive));
            Assert.IsFalse(SaveSlots.ManualAllowed(GameState.ExpeditionActive, GameState.CampManagement));
            Assert.IsFalse(SaveSlots.ManualAllowed(GameState.RaidActive, GameState.CampManagement));
            Assert.AreNotEqual("save.camp_only", Loc.Raw("save.camp_only", "es"));
        }
    }
}

using NUnit.Framework;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class SaveRegistryTests
    {
        sealed class Part : ISaveable
        {
            public Part(string id) { SaveId = id; }
            public string SaveId { get; }
            public string State = "";
            public int Restores;
            public string CaptureState() => State;
            public void RestoreState(string state) { State = state; Restores++; }
        }

        [SetUp]
        public void Reset() => SaveRegistry.Clear();

        [TearDown]
        public void Tidy() => SaveRegistry.Clear();

        [Test]
        public void PartsAreWrittenInIdOrderAndReadBackByTheirId()
        {
            var radio = new Part("radio") { State = "3" };
            var codex = new Part("codex") { State = "h:move" };
            Assert.IsTrue(SaveRegistry.Register(radio));
            Assert.IsTrue(SaveRegistry.Register(codex));
            var blobs = SaveRegistry.Capture();
            Assert.AreEqual(2, blobs.Length);
            Assert.AreEqual("codex", blobs[0].id);
            Assert.AreEqual("radio", blobs[1].id);

            radio.State = "";
            codex.State = "";
            SaveRegistry.Restore(blobs);
            Assert.AreEqual("3", radio.State);
            Assert.AreEqual("h:move", codex.State);
        }

        [Test]
        public void ASecondClaimOnAnIdIsRefused()
        {
            var first = new Part("codex");
            Assert.IsTrue(SaveRegistry.Register(first));
            Assert.IsTrue(SaveRegistry.Register(first));
            Assert.IsFalse(SaveRegistry.Register(new Part("codex")));
            Assert.IsFalse(SaveRegistry.Register(new Part("")));
            Assert.AreEqual(1, SaveRegistry.Count);
            SaveRegistry.Unregister(new Part("codex"));
            Assert.AreEqual(1, SaveRegistry.Count);
            SaveRegistry.Unregister(first);
            Assert.AreEqual(0, SaveRegistry.Count);
        }

        [Test]
        public void APartLoadedBeforeItsOwnerWaitsAndIsKeptOnResave()
        {
            SaveRegistry.Restore(new[] { new SaveBlob { id = "street", state = "lot7" } });
            var resaved = SaveRegistry.Capture();
            Assert.AreEqual(1, resaved.Length);
            Assert.AreEqual("lot7", resaved[0].state);

            var street = new Part("street");
            SaveRegistry.Register(street);
            Assert.AreEqual("lot7", street.State);
            Assert.AreEqual(1, street.Restores);
        }

        [Test]
        public void ALivePartMissingFromTheSaveIsReset()
        {
            var codex = new Part("codex") { State = "h:move" };
            SaveRegistry.Register(codex);
            SaveRegistry.Restore(new[] { new SaveBlob { id = "codex", state = "a" }, new SaveBlob { id = "codex", state = "b" }, null });
            Assert.AreEqual("a", codex.State);
            SaveRegistry.Restore(null);
            Assert.AreEqual("", codex.State);
        }

        [Test]
        public void TheCodexFieldMovesIntoParts()
        {
            var old = new SaveGameData { schemaVersion = 2, codex = "h:move,e:zombie.walker" };
            var upgraded = SaveMigrations.Upgrade(old);
            Assert.AreEqual(SaveCodec.CurrentSchema, upgraded.schemaVersion);
            Assert.AreEqual("", upgraded.codex);
            Assert.AreEqual("h:move,e:zombie.walker", SaveRegistry.Find(upgraded.parts, CodexDirector.SaveKey));

            var already = SaveMigrations.Upgrade(new SaveGameData
            {
                schemaVersion = 2,
                codex = "stale",
                parts = new[] { new SaveBlob { id = CodexDirector.SaveKey, state = "fresh" } }
            });
            Assert.AreEqual("fresh", SaveRegistry.Find(already.parts, CodexDirector.SaveKey));
            Assert.AreEqual(1, already.parts.Length);
            Assert.IsNull(SaveRegistry.Find(already.parts, "radio"));
        }

        [Test]
        public void PartsSurviveTheCodec()
        {
            var upgraded = SaveMigrations.Upgrade(new SaveGameData { schemaVersion = 2, codex = "h:move,e:zombie.walker" });
            string json = SaveCodec.Serialize(upgraded);
            StringAssert.Contains("\"parts\"", json);
            Assert.IsTrue(SaveCodec.TryDeserialize(json, out var loaded, out var error), error);
            Assert.AreEqual("h:move,e:zombie.walker", SaveRegistry.Find(loaded.parts, CodexDirector.SaveKey));
            CollectionAssert.IsEmpty(SaveDiff.Compare(upgraded, loaded));
        }
    }
}

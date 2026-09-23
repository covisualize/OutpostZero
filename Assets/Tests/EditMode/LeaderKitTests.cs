using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class LeaderKitTests
    {
        private static LeaderKit.Kit Sample()
        {
            return new LeaderKit.Kit
            {
                Gear = "bandage*3+canned_food*2+scrap*14+medkit*1",
                Belt = new[] { "bandage", "", "molotov", "" },
                Arms = new List<LeaderKit.Arm>
                {
                    new LeaderKit.Arm { Id = "pistol_9mm", Magazine = 7, Reserve = 36 },
                    new LeaderKit.Arm { Id = "machete", Magazine = 0, Reserve = 0 },
                    new LeaderKit.Arm { Id = "shotgun_pump", Magazine = 2, Reserve = 11 },
                },
                Active = 2
            };
        }

        [Test]
        public void TheKitRoundTrips()
        {
            string packed = LeaderKit.Pack(Sample());
            Assert.IsTrue(LeaderKit.TryUnpack(packed, out var kit), packed);
            Assert.AreEqual(Sample().Gear, kit.Gear);
            CollectionAssert.AreEqual(Sample().Belt, kit.Belt);
            Assert.AreEqual(3, kit.Arms.Count);
            Assert.AreEqual("shotgun_pump", kit.Arms[2].Id);
            Assert.AreEqual(2, kit.Arms[2].Magazine);
            Assert.AreEqual(11, kit.Arms[2].Reserve);
            Assert.AreEqual(2, kit.Active);
            Assert.AreEqual(packed, LeaderKit.Pack(kit), "packing is stable");
        }

        [Test]
        public void AnEmptyOrBrokenPartLeavesTheLeaderAlone()
        {
            Assert.IsFalse(LeaderKit.TryUnpack("", out _), "an older save has no kit part");
            Assert.IsFalse(LeaderKit.TryUnpack(null, out _));
            Assert.IsFalse(LeaderKit.TryUnpack("bandage*3", out _));
        }

        [Test]
        public void BadEntriesAreDroppedAndTheActiveSlotStaysInRange()
        {
            Assert.IsTrue(LeaderKit.TryUnpack("//pistol_9mm:x:3,rifle_assault:10:40,:1:1,smg:5/9", out var kit));
            Assert.AreEqual(1, kit.Arms.Count);
            Assert.AreEqual("rifle_assault", kit.Arms[0].Id);
            Assert.AreEqual(0, kit.Active);
            Assert.AreEqual(4, kit.Belt.Length);
            Assert.AreEqual("", kit.Gear);
        }

        [Test]
        public void NegativeRoundsAndSeparatorsInIdsCannotCorruptThePart()
        {
            var kit = Sample();
            kit.Arms[0] = new LeaderKit.Arm { Id = "pis/tol:9,mm", Magazine = -4, Reserve = -1 };
            kit.Belt[1] = "ban,dage";
            Assert.IsTrue(LeaderKit.TryUnpack(LeaderKit.Pack(kit), out var back));
            Assert.AreEqual("pistol9mm", back.Arms[0].Id);
            Assert.AreEqual(0, back.Arms[0].Magazine);
            Assert.AreEqual(0, back.Arms[0].Reserve);
            Assert.AreEqual("bandage", back.Belt[1]);
            Assert.AreEqual(3, back.Arms.Count);
        }

        [Test]
        public void TheLeaderCarriesTheKitPart()
        {
            string root = Directory.GetCurrentDirectory();
            string controller = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Player", "PlayerController.cs"));
            StringAssert.Contains("Attach.Ensure<LeaderKitSave>(gameObject)", controller);
            string part = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Player", "LeaderKitSave.cs"));
            StringAssert.Contains("SaveRegistry.Register(this)", part);
            StringAssert.Contains("SaveRegistry.Unregister(this)", part);
            Assert.AreEqual("leader_kit", LeaderKit.SaveId, "save ids never change between builds");
        }

        private sealed class Fake : ISaveable
        {
            public string SaveId => LeaderKit.SaveId;
            public string Held = "";
            public string CaptureState() => Held;
            public void RestoreState(string state) => Held = state;
        }

        [Test]
        public void TheKitTravelsThroughTheSaveParts()
        {
            SaveRegistry.Clear();
            try
            {
                var leader = new Fake { Held = LeaderKit.Pack(Sample()) };
                Assert.IsTrue(SaveRegistry.Register(leader));
                var parts = SaveRegistry.Capture();
                leader.Held = "";
                SaveRegistry.Restore(parts);
                Assert.AreEqual(LeaderKit.Pack(Sample()), leader.Held);
            }
            finally
            {
                SaveRegistry.Clear();
            }
        }
    }
}

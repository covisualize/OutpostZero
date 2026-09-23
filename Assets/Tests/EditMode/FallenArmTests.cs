using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-60: the fallen leader's personal weapon stays on the body with its rounds and comes back when the body is found.
    /// </summary>
    public class FallenArmTests
    {
        static string Root => Directory.GetCurrentDirectory();

        static List<LeaderKit.Arm> Arms(params string[] ids)
        {
            var list = new List<LeaderKit.Arm>();
            foreach (var id in ids) list.Add(new LeaderKit.Arm { Id = id, Magazine = 7, Reserve = 21 });
            return list;
        }

        [Test]
        public void TheWeaponInHandStaysUnlessItIsTheOnlyOne()
        {
            Assert.AreEqual(1, SuccessionLedger.Personal(Arms("pistol_9mm", "shotgun_pump", "machete"), 1));
            Assert.AreEqual(2, SuccessionLedger.Personal(Arms("pistol_9mm", "shotgun_pump", "machete"), 9));
            Assert.AreEqual(-1, SuccessionLedger.Personal(Arms("pistol_9mm"), 0), "the heir keeps a lone weapon");
            Assert.AreEqual(-1, SuccessionLedger.Personal(null, 0));
        }

        [Test]
        public void TheArmRidesInTheBodysGearAndSplitsBackOut()
        {
            var arm = new LeaderKit.Arm { Id = "shotgun_pump", Magazine = 4, Reserve = 12 };
            string gear = SuccessionLedger.WithArm("bandage*2+scrap*5", arm);
            Assert.AreEqual("bandage*2+scrap*5+@shotgun_pump:4:12", gear);
            Assert.IsTrue(SuccessionLedger.SplitArm(gear, out string pack, out var back));
            Assert.AreEqual("bandage*2+scrap*5", pack);
            Assert.AreEqual("shotgun_pump", back.Id);
            Assert.AreEqual(4, back.Magazine);
            Assert.AreEqual(12, back.Reserve);

            Assert.AreEqual("@pistol_9mm:0:0", SuccessionLedger.WithArm("", new LeaderKit.Arm { Id = "pistol_9mm", Magazine = -3 }));
            Assert.IsFalse(SuccessionLedger.SplitArm("bandage*2", out pack, out _));
            Assert.AreEqual("bandage*2", pack);
            Assert.IsFalse(SuccessionLedger.SplitArm("@broken+cloth*1", out pack, out _));
            Assert.AreEqual("@broken+cloth*1", pack);
        }

        [Test]
        public void TheArmSurvivesTheCorpseSave()
        {
            var marks = new List<SuccessionLedger.CorpseMark>
            {
                new SuccessionLedger.CorpseMark { district = "ash_market", x = 1f, y = 0f, z = 2f, name = "Mara", gear = "cloth*1+@rifle_assault:12:30" }
            };
            var back = SuccessionLedger.UnpackCorpses(SuccessionLedger.PackCorpses(marks));
            Assert.AreEqual(1, back.Count);
            Assert.IsTrue(SuccessionLedger.SplitArm(back[0].gear, out _, out var arm));
            Assert.AreEqual("rifle_assault", arm.Id);
            Assert.AreEqual(30, arm.Reserve);
        }

        [Test]
        public void DeathLeavesTheArmAndTheBodyHandsItBack()
        {
            string roster = File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Colony/SurvivorRoster.cs"));
            StringAssert.Contains("SuccessionLedger.Personal(arms, hands.ActiveSlot)", roster);
            StringAssert.Contains("hands.RestoreArms(arms, 0)", roster);
            string ledger = File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Colony/SuccessionLedger.cs"));
            StringAssert.Contains("SuccessionLedger.SplitArm(gear, out string pack, out var arm)", ledger);
            StringAssert.Contains("player.TakeFromGround(arm.Id, arm.Magazine, arm.Reserve", ledger);
        }
    }
}

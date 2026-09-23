using System.IO;
using NUnit.Framework;
using OutpostZero.Items;

namespace OutpostZero.Tests.EditMode
{
    public class LootSweepTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void OfferNamesTheCountAndTheWholeStackWeight()
        {
            Assert.AreEqual("Take 12× 9mm (0.60 kg)", ItemBrief.Offer("Take", "9mm", 12, 0.05f));
            Assert.AreEqual("Take Bandage (0.10 kg)", ItemBrief.Offer("Take", "Bandage", 1, 0.1f));
            Assert.AreEqual("Take 3× Rounds", ItemBrief.Offer("Take", "Rounds", 3, 0f));
            Assert.AreEqual("Take Scrap (0.10 kg)", ItemBrief.Offer("Take", "Scrap", 0, 0.1f));
        }

        [Test]
        public void AHeavyPackStopsTheSprint()
        {
            Assert.IsTrue(PackOps.AllowsSprint(31f, 35f));
            Assert.IsFalse(PackOps.AllowsSprint(32f, 35f));
            Assert.IsFalse(PackOps.AllowsSprint(35f, 35f));
            Assert.IsTrue(PackOps.AllowsSprint(44f, 50f));
        }

        [Test]
        public void HoldingInteractForHalfASecondSweeps()
        {
            Assert.IsFalse(PackOps.Swept(0.49f));
            Assert.IsTrue(PackOps.Swept(PackOps.SweepHold));
        }

        [Test]
        public void TheLeaderAndTheInteractorUseTheRules()
        {
            string controller = Read("Assets/Scripts/Player/PlayerController.cs");
            StringAssert.Contains("PackOps.AllowsSprint(inventory.CurrentWeight, inventory.MaxWeightCapacity)", controller);
            string hands = Read("Assets/Scripts/Player/PlayerInteractor.cs");
            StringAssert.Contains("ExpeditionInput.InteractHeld", hands);
            StringAssert.Contains("PackOps.Swept(sweep)", hands);
            StringAssert.Contains("open.TakeAll(inventory)", hands);
            string item = Read("Assets/Scripts/Items/WorldItem.cs");
            StringAssert.Contains("ItemBrief.Offer(", item);
        }
    }
}
